using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using Alchemist.AlchemistCode.Cards;

namespace Alchemist.AlchemistCode.Commands;

/// <summary>
/// The Infuse mechanic: enchant a card until the end of combat.
///   Attack → Sharp (+damage per hit — scales on multi-hit attacks).
///   Skill → Nimble (+block per EACH block gain — scales on multi-block skills) if it gains Block,
///     else Adroit (+block once on play).
///   Power → Swift (draw on play).
///   Curse/Status/Quest → Ethereal keyword (exhausts if unplayed).
/// Re-Infusing a card stacks the enchantment amount (CardCmd.Enchant sums same-type amounts).
/// Enchantments are run-permanent by default, so infused cards are tracked and cleared at combat
/// end by <see cref="Patches.InfusionCombatEndPatch"/>.
/// </summary>
public static class Infusion
{
    private const int Amount = 3;       // Attack → Sharp, Skill → Adroit
    private const int SwiftAmount = 2;  // Power → Swift (a drawn Power is high value, so a touch weaker)
    private static readonly LocString SelectPrompt = new("card_keywords", "ALCHEMIST-INFUSE.selectionPrompt");

    // Cards enchanted by Infuse this combat, so we can strip the (combat-only) enchantment at end.
    private static readonly HashSet<CardModel> Infused = new();

    /// <summary>Let the player choose <paramref name="count"/> cards from a pile and Infuse each.</summary>
    public static async Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile, int count)
    {
        var prefs = new CardSelectorPrefs(SelectPrompt, count);
        var picks = (pile == PileType.Hand
            ? await CardSelectCmd.FromHand(ctx, source.Owner, prefs, null, source)
            : await CardSelectCmd.FromCombatPile(ctx, pile.GetPile(source.Owner), source.Owner, prefs)).ToList();
        foreach (var card in picks)
            Infuse(card);
        // Draw/Discard infusions hit cards the player can't see (and the selector auto-picks a lone
        // card with no UI), so pop the infused cards up. Hand picks are already visible — no popup.
        if (pile is PileType.Draw or PileType.Discard && picks.Count > 0)
            CardCmd.Preview(picks);
    }

    /// <summary>Let the player Infuse any number of cards from their Hand (GUARDS-style 0..∞ selector).</summary>
    public static async Task InfuseAnyFromHand(PlayerChoiceContext ctx, AlchemistCard source)
    {
        var prefs = new CardSelectorPrefs(SelectPrompt, 0, 999999999);
        var picks = await CardSelectCmd.FromHand(ctx, source.Owner, prefs, null, source);
        foreach (var card in picks)
            Infuse(card);
    }

    /// <summary>Infuse <paramref name="count"/> random cards from <paramref name="owner"/>'s Hand
    /// (optionally excluding one card, e.g. the source that triggered it).</summary>
    public static void InfuseRandomFromHand(Player owner, int count, CardModel? exclude = null)
    {
        var rng = owner.RunState.Rng.CombatCardGeneration;
        var hand = PileType.Hand.GetPile(owner).Cards.Where(c => c != exclude).ToList();
        var infused = new List<CardModel>();
        for (var i = 0; i < count && hand.Count > 0; i++)
        {
            var card = hand[rng.NextInt(hand.Count)];
            hand.Remove(card);
            Infuse(card);
            infused.Add(card);
        }
        // Random picks are "hidden" (the player didn't choose them) — pop them up so it's clear which
        // cards got infused.
        if (infused.Count > 0)
            CardCmd.Preview(infused);
    }

    public static void Infuse(CardModel card)
    {
        if (card.Type is CardType.Curse or CardType.Status or CardType.Quest)
        {
            card.AddKeyword(CardKeyword.Ethereal); // junk: exhaust it if unplayed (self-resolving, kept)
            return;
        }

        switch (card.Type)
        {
            case CardType.Attack:
                TryEnchant<Sharp>(card, Amount); // always Sharp — scales per hit on multi-hit attacks
                break;
            case CardType.Skill:
                // A Skill that gains Block → Nimble: unlike Adroit (Block once on play), Nimble's bonus is
                // added to EACH block gain, so a multi-block skill scales it up — the block analog of Sharp
                // on a multi-hit attack. Skills with no Block fall back to Adroit.
                if (card.GainsBlock) TryEnchant<Nimble>(card, Amount);
                else TryEnchant<Adroit>(card, Amount);
                break;
            case CardType.Power:
                TryEnchant<Swift>(card, SwiftAmount);
                break;
        }
    }

    private static void TryEnchant<T>(CardModel card, int amount) where T : EnchantmentModel
    {
        // Re-Infusing the same card should STACK the amount. The base Sharp/Adroit/Swift enchantments are
        // NOT IsStackable, so CardCmd.Enchant's CanEnchant refuses a second application and the amount would
        // stay put (the bug). So when we've already Infused this card with the same enchantment, clear it and
        // re-apply the summed amount. Only stack onto OUR infusions — never a pre-existing/permanent one.
        if (card.Enchantment is T existing)
        {
            if (!Infused.Contains(card)) return; // a permanent/foreign enchantment of the same type — leave it
            var summed = existing.Amount + amount;
            CardCmd.ClearEnchantment(card);
            CardCmd.Enchant<T>(card, summed);
            return; // already tracked in Infused
        }
        if (card.Enchantment != null) return;                  // a different enchantment type — don't cross-stack
        if (!ModelDb.Enchantment<T>().CanEnchant(card)) return; // honor CanEnchant (card type, GainsBlock, etc.)
        CardCmd.Enchant<T>(card, amount);
        Infused.Add(card);
    }

    /// <summary>Called at combat end — removes every combat-only Infusion.</summary>
    public static void ClearCombatInfusions()
    {
        foreach (var card in Infused)
            if (card.Enchantment != null)
                CardCmd.ClearEnchantment(card);
        Infused.Clear();
    }
}
