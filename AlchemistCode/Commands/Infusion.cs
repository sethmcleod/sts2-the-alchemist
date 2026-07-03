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
///   Attack → Sharp (+damage), Skill → Adroit (+block on play), Power → Swift (draw on play).
///   Curse/Status/Quest → Ethereal keyword (exhausts if unplayed).
/// Re-Infusing a card stacks the enchantment amount (CardCmd.Enchant sums same-type amounts).
/// Enchantments are run-permanent by default, so infused cards are tracked and cleared at combat
/// end by <see cref="Patches.InfusionCombatEndPatch"/>.
/// </summary>
public static class Infusion
{
    private const int Amount = 2;
    private static readonly LocString SelectPrompt = new("card_keywords", "ALCHEMIST-INFUSE.selectionPrompt");

    // Cards enchanted by Infuse this combat, so we can strip the (combat-only) enchantment at end.
    private static readonly HashSet<CardModel> Infused = new();

    /// <summary>Let the player choose <paramref name="count"/> cards from a pile and Infuse each.</summary>
    public static async Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile, int count)
    {
        var prefs = new CardSelectorPrefs(SelectPrompt, count);
        var picks = pile == PileType.Hand
            ? await CardSelectCmd.FromHand(ctx, source.Owner, prefs, null, source)
            : await CardSelectCmd.FromCombatPile(ctx, pile.GetPile(source.Owner), source.Owner, prefs);
        foreach (var card in picks)
            Infuse(card);
    }

    /// <summary>Infuse <paramref name="count"/> random cards from <paramref name="owner"/>'s Hand
    /// (optionally excluding one card, e.g. the source that triggered it).</summary>
    public static void InfuseRandomFromHand(Player owner, int count, CardModel? exclude = null)
    {
        var rng = owner.RunState.Rng.CombatCardGeneration;
        var hand = PileType.Hand.GetPile(owner).Cards.Where(c => c != exclude).ToList();
        for (var i = 0; i < count && hand.Count > 0; i++)
        {
            var card = hand[rng.NextInt(hand.Count)];
            hand.Remove(card);
            Infuse(card);
        }
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
            case CardType.Attack: TryEnchant<Sharp>(card); break;
            case CardType.Skill: TryEnchant<Adroit>(card); break;
            case CardType.Power: TryEnchant<Swift>(card); break;
        }
    }

    private static void TryEnchant<T>(CardModel card) where T : EnchantmentModel
    {
        // Can't stack onto a different (e.g. permanent) enchantment; and honor CanEnchant restrictions.
        if (card.Enchantment != null && card.Enchantment is not T) return;
        if (!ModelDb.Enchantment<T>().CanEnchant(card)) return;
        CardCmd.Enchant<T>(card, Amount); // stacks the amount on repeat Infusions
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
