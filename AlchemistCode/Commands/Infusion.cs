using Alchemist.AlchemistCode.Powers;
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

// Infuse enchants a card until the end of combat, but an enchantment is run-permanent by default. So this
// class tracks the infused cards and Patches.InfusionCombatEndPatch clears them at combat end
public static class Infusion
{
    // Null for the types that take the Ethereal keyword instead
    private static Type? EnchantTypeFor(CardModel card) => card.Type switch
    {
        CardType.Attack => typeof(Laced),
        CardType.Skill => typeof(Dosed),
        CardType.Power => typeof(Fortified),
        _ => null,
    };

    // CardSelectorPrefs injects {Amount}, {MinCount}, and {MaxCount}. The unbounded prompt prints no
    // count, because AnyNumber would render as-is
    private static LocString SelectPrompt => new("card_keywords", "ALCHEMIST-INFUSE.selectionPrompt");
    private static LocString SelectPromptRange => new("card_keywords", "ALCHEMIST-INFUSE.selectionPromptRange");
    private static LocString SelectPromptAny => new("card_keywords", "ALCHEMIST-INFUSE.selectionPromptAny");

    private static readonly HashSet<CardModel> Infused = new();

    // Clearing an enchantment does not remove a keyword, so track the cards that took Ethereal separately
    private static readonly HashSet<CardModel> AddedEthereal = new();

    // Fed by the shared CardCmd.Enchant hook, so the Masterwork threshold counts enchantments from other
    // mods too, not Infuse alone
    private static readonly HashSet<CardModel> EnchantedThisCombat = new();

    // The tips and the enchant share these, and FromEnchantment defaults to 1, so a tip that does not
    // pass one goes stale in silence
    private const int LacedAmount = 1;
    private const int DosedAmount = 2;
    private const int FortifiedAmount = 2;

    // Take(1) keeps each enchantment's own tip and drops its extras, which is where Dosed explains
    // Antitoxin. The power is invisible so there is no icon to hover either, so it is added back once
    public static IEnumerable<IHoverTip> InfuseTips() =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) }
            .Concat(HoverTipFactory.FromEnchantment<Laced>(LacedAmount).Take(1))
            .Concat(HoverTipFactory.FromEnchantment<Dosed>(DosedAmount).Take(1))
            .Concat(HoverTipFactory.FromEnchantment<Fortified>(FortifiedAmount).Take(1))
            .Append(HoverTipFactory.FromPower<AntitoxinPower>());

    public static void RecordCombatEnchant(CardModel card) => EnchantedThisCombat.Add(card);

    public static int EnchantedThisCombatCount(Player owner) => EnchantedThisCombat.Count(c => c.Owner == owner);

    // Masterwork counts distinct cards, so a re-infuse of one already counted adds nothing to its tally
    public static bool WouldNewlyEnchant(CardModel card) => CanInfuse(card) && !EnchantedThisCombat.Contains(card);

    public static bool CanInfuse(CardModel card)
    {
        if (card.Type is CardType.Curse or CardType.Status or CardType.Quest)
            return !card.Keywords.Contains(CardKeyword.Ethereal);
        // Laced keys on IsPoweredAttack, so an Unpowered attacker would take a visible Laced icon
        // and a promise of bonus damage that never fires
        if (card is AlchemistCard { DealsUnpoweredDamage: true }) return false;
        if (EnchantTypeFor(card) is null) return false;
        // Enchantments do not stack, matching every base-game one, so an enchanted card is not a target
        return card.Enchantment == null;
    }

    // A fixed-count selection resolves with no screen when no more cards match than the count. The caller
    // then previews the result, as the base game does for an Armaments upgrade that auto-resolves
    internal static bool HandSelectIsAutomatic(Player owner, Func<CardModel, bool> filter, int min, int max)
    {
        if (min != max) return false;
        var matches = PileType.Hand.GetPile(owner).Cards.Count(filter);
        return matches > 0 && matches <= min;
    }

    public static Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile, int count) =>
        InfuseChosen(ctx, source, pile, count, count);

    // For a non-card source such as a potion, which has no pile of its own
    public static async Task InfuseChosenFromHand(PlayerChoiceContext ctx, AbstractModel source, Player owner,
        int min, int max)
    {
        var prompt = max >= AlchemistCard.AnyNumber ? SelectPromptAny
            : min == max ? SelectPrompt
            : SelectPromptRange;
        var autoResolved = HandSelectIsAutomatic(owner, CanInfuse, min, max);
        var prefs = new CardSelectorPrefs(prompt, min, max);
        var picks = (await CardSelectCmd.FromHand(ctx, owner, prefs, CanInfuse, source)).ToList();
        foreach (var card in picks)
            Infuse(card);
        // No screen was shown, so preview the picks to make the automatic infuse visible
        if (autoResolved && picks.Count > 0)
            CardCmd.Preview(picks);
    }

    public static async Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile,
        int min, int max)
    {
        var prompt = max >= AlchemistCard.AnyNumber ? SelectPromptAny
            : min == max ? SelectPrompt
            : SelectPromptRange;
        var autoResolved = pile == PileType.Hand && HandSelectIsAutomatic(source.Owner, CanInfuse, min, max);
        var prefs = new CardSelectorPrefs(prompt, min, max);
        var picks = (pile == PileType.Hand
                ? await CardSelectCmd.FromHand(ctx, source.Owner, prefs, CanInfuse, source)
                : await CardSelectCmd.FromCombatPile(ctx, pile.GetPile(source.Owner), source.Owner, prefs, CanInfuse))
            .ToList();
        foreach (var card in picks)
            Infuse(card);
        // Draw and discard picks happen off screen, and an automatic hand pick shows no screen. Only a
        // manual hand pick has already shown the player the card
        if (picks.Count > 0 && (pile is PileType.Draw or PileType.Discard || autoResolved))
            CardCmd.Preview(picks);
    }

    // Bestow uses this to infuse a teammate hand, which the caster cannot see to target
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
        // The player cannot see random picks, so show them
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
                TryEnchant<Laced>(card, LacedAmount);
                break;
            case CardType.Skill:
                TryEnchant<Dosed>(card, DosedAmount);
                break;
            case CardType.Power:
                TryEnchant<Fortified>(card, FortifiedAmount);
                break;
        }
    }

    private static void TryEnchant<T>(CardModel card, int amount) where T : EnchantmentModel
    {
        if (!ModelDb.Enchantment<T>().CanEnchant(card)) return;
        CardCmd.Enchant<T>(card, amount);
        Infused.Add(card);
    }

    // Every card here is guarded on its own. These sets hold references that can go stale: a card that was
    // transformed or removed from state during combat, or one left over from an earlier run. Clearing a
    // stale card throws, and this runs inside Hook.AfterCombatEnd, which the game calls in the middle of
    // its combat teardown. A throw there aborts the rest of that teardown, and the run then behaves as if
    // combat never finished, which silently disables every later CardCmd.Upgrade and CardCmd.Transform
    public static void ClearCombatInfusions()
    {
        foreach (var card in Infused)
            Guarded(card, c =>
            {
                if (c.Enchantment != null) CardCmd.ClearEnchantment(c);
            });
        foreach (var card in AddedEthereal)
            Guarded(card, c => c.RemoveKeyword(CardKeyword.Ethereal));

        ResetTracking();
    }

    /// <summary>Drops references from the run that just ended, so a new or loaded run starts clean.</summary>
    public static void ResetTracking()
    {
        Infused.Clear();
        AddedEthereal.Clear();
        EnchantedThisCombat.Clear();
    }

    private static void Guarded(CardModel card, Action<CardModel> action)
    {
        try
        {
            // Reading Enchantment or writing a keyword on a canonical model throws
            if (card is { IsMutable: true }) action(card);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[Infuse] Skipped a stale card at combat end: {e.Message}");
        }
    }
}
