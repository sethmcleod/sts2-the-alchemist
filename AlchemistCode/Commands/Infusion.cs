using System;
using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Commands;

// Infuse: enchant a card until end of combat. Enchantments are run-permanent by default, so infused
// cards are tracked here and cleared at combat end by Patches.InfusionCombatEndPatch.
// Each Infuse adds 1 to the enchantment's amount, and re-infusing a card stacks that amount.
public static class Infusion
{
    // The enchantment applied for each card type; null for types that get the Ethereal keyword instead
    private static Type? EnchantTypeFor(CardModel card) => card.Type switch
    {
        CardType.Attack => typeof(Toxic),
        CardType.Skill => typeof(Fuming),
        CardType.Power => typeof(Exalted),
        _ => null,
    };

    // Exact-count prompt ("Choose a card"/"Choose 2 cards"); the range prompt ("Choose up to N cards") is used
    // when the player may pick fewer than the max. CardSelectorPrefs injects {Amount}/{MinCount}/{MaxCount}.
    private static LocString SelectPrompt => new("card_keywords", "ALCHEMIST-INFUSE.selectionPrompt");
    private static LocString SelectPromptRange => new("card_keywords", "ALCHEMIST-INFUSE.selectionPromptRange");

    private static readonly HashSet<CardModel> Infused = new();

    // Curses get Ethereal as a keyword, and clearing an enchantment never undoes a keyword, so remember only
    // the cards we added it to, and strip it at combat end
    private static readonly HashSet<CardModel> AddedEthereal = new();

    // Distinct cards enchanted (by any source) during the current combat. Recorded from the shared
    // CardCmd.Enchant hook so Masterwork's threshold counts other mods' enchantments too, not just Infuse
    private static readonly HashSet<CardModel> EnchantedThisCombat = new();

    // The Infuse keyword tip plus a tip for each enchantment it can grant. Default amount 1 = one Infuse
    public static IEnumerable<IHoverTip> InfuseTips() =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) }
            .Concat(HoverTipFactory.FromEnchantment<Toxic>().Take(1))
            .Concat(HoverTipFactory.FromEnchantment<Fuming>().Take(1))
            .Concat(HoverTipFactory.FromEnchantment<Exalted>().Take(1));

    public static void RecordCombatEnchant(CardModel card) => EnchantedThisCombat.Add(card);

    public static int EnchantedThisCombatCount(Player owner) => EnchantedThisCombat.Count(c => c.Owner == owner);

    // Infusable if it takes the Ethereal keyword, or if applying its type's enchantment would stack cleanly
    // (no enchantment yet, or already the same one). Re-infusing a card grows its amount
    public static bool CanInfuse(CardModel card)
    {
        if (card.Type is CardType.Curse or CardType.Status or CardType.Quest)
            return !card.Keywords.Contains(CardKeyword.Ethereal);
        if (EnchantTypeFor(card) is not { } type) return false;
        return card.Enchantment == null || card.Enchantment.GetType() == type;
    }

    public static Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile, int count) =>
        InfuseChosen(ctx, source, pile, count, count);

    // min/max lets the player choose how many to infuse: "up to N" (0..N) or "any number" (0..huge)
    public static async Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile,
        int min, int max)
    {
        var prompt = min == max ? SelectPrompt : SelectPromptRange;
        var prefs = new CardSelectorPrefs(prompt, min, max);
        var picks = (pile == PileType.Hand
            ? await CardSelectCmd.FromHand(ctx, source.Owner, prefs, CanInfuse, source)
            : await CardSelectCmd.FromCombatPile(ctx, pile.GetPile(source.Owner), source.Owner, prefs, CanInfuse))
            .ToList();
        foreach (var card in picks)
            Infuse(card);
        // Draw/discard picks aren't visible to the player, so pop them up; hand picks already are
        if (pile is PileType.Draw or PileType.Discard && picks.Count > 0)
            CardCmd.Preview(picks);
    }

    // Used by Bestow to infuse a teammate's hand, which the caster can't see to target
    public static void InfuseRandomFromHand(Player owner, int count, CardModel? exclude = null)
    {
        var rng = owner.RunState.Rng.CombatCardGeneration;
        var hand = PileType.Hand.GetPile(owner).Cards.Where(c => c != exclude && CanInfuse(c)).ToList();
        var infused = new List<CardModel>();
        for (var i = 0; i < count && hand.Count > 0; i++)
        {
            var card = hand[rng.NextInt(hand.Count)];
            hand.Remove(card);
            Infuse(card);
            infused.Add(card);
        }
        // Random picks aren't visible to the player, so pop them up
        if (infused.Count > 0)
            CardCmd.Preview(infused);
    }

    public static void Infuse(CardModel card)
    {
        if (!CanInfuse(card)) return;

        if (card.Type is CardType.Curse or CardType.Status or CardType.Quest)
        {
            card.AddKeyword(CardKeyword.Ethereal);
            AddedEthereal.Add(card);
            Infused.Add(card);
            return;
        }

        switch (card.Type)
        {
            case CardType.Attack:
                TryEnchant<Toxic>(card);
                break;
            case CardType.Skill:
                TryEnchant<Fuming>(card);
                break;
            case CardType.Power:
                TryEnchant<Exalted>(card);
                break;
        }
    }

    // Enchant adds `amount` to the enchantment (stacking if the card already has the same one)
    private static void TryEnchant<T>(CardModel card) where T : EnchantmentModel
    {
        if (!ModelDb.Enchantment<T>().CanEnchant(card)) return;
        CardCmd.Enchant<T>(card, 1);
        Infused.Add(card);
    }

    public static void ClearCombatInfusions()
    {
        foreach (var card in Infused)
            if (card.Enchantment != null)
                CardCmd.ClearEnchantment(card);
        foreach (var card in AddedEthereal)
            card.RemoveKeyword(CardKeyword.Ethereal);

        Infused.Clear();
        AddedEthereal.Clear();
        EnchantedThisCombat.Clear();
    }
}
