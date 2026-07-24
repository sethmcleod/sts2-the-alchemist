using System;
using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Config;
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

// Infuse enchants a card until the end of combat. An enchantment is run-permanent by default. This
// class tracks the infused cards, and Patches.InfusionCombatEndPatch clears them at combat end.
// Each Infuse adds 1 to the enchantment amount. A second Infuse on the same card stacks that amount
public static class Infusion
{
    // The enchantment applied for each card type; null for types that get the Ethereal keyword instead
    private static Type? EnchantTypeFor(CardModel card) => card.Type switch
    {
        CardType.Attack => typeof(Laced),
        CardType.Skill => typeof(Fuming),
        CardType.Power => typeof(Exalted),
        _ => null,
    };

    // Exact-count prompt ("Choose a card"/"Choose 2 cards"); the range prompt ("Choose up to N cards") is used
    // when the player may pick fewer than the max. CardSelectorPrefs injects {Amount}/{MinCount}/{MaxCount}.
    // The unbounded prompt ("Choose cards") prints no count, because an unbounded max is a sentinel number
    private static LocString SelectPrompt => new("card_keywords", "ALCHEMIST-INFUSE.selectionPrompt");
    private static LocString SelectPromptRange => new("card_keywords", "ALCHEMIST-INFUSE.selectionPromptRange");
    private static LocString SelectPromptAny => new("card_keywords", "ALCHEMIST-INFUSE.selectionPromptAny");

    private static readonly HashSet<CardModel> Infused = new();

    // A Curse gets Ethereal as a keyword. A clear of the enchantment does not remove a keyword. Record
    // only the cards that got Ethereal here, and remove it at combat end
    private static readonly HashSet<CardModel> AddedEthereal = new();

    // The distinct cards that any source enchanted in the current combat. The shared CardCmd.Enchant hook
    // records them, so the Masterwork threshold also counts enchantments from other mods, not only Infuse
    private static readonly HashSet<CardModel> EnchantedThisCombat = new();

    // The amount of each enchantment that one Infuse grants. The tips and the enchant use these same constants,
    // and FromEnchantment defaults to 1, so a tip that does not pass one of these goes stale in silence
    private const int LacedAmount = 2;
    private const int FumingAmount = 1;
    private const int ExaltedAmount = 1;

    // The Infuse keyword tip, plus one tip for each enchantment that Infuse can grant, each at the
    // amount that one Infuse gives
    public static IEnumerable<IHoverTip> InfuseTips() =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) }
            .Concat(HoverTipFactory.FromEnchantment<Laced>(LacedAmount).Take(1))
            .Concat(HoverTipFactory.FromEnchantment<Fuming>(FumingAmount).Take(1))
            .Concat(HoverTipFactory.FromEnchantment<Exalted>(ExaltedAmount).Take(1));

    public static void RecordCombatEnchant(CardModel card) => EnchantedThisCombat.Add(card);

    public static int EnchantedThisCombatCount(Player owner) => EnchantedThisCombat.Count(c => c.Owner == owner);

    // True when infusing this card would add a NEW card to the combat enchant tally. Masterwork counts the
    // distinct cards Enchanted this combat, so a re-infuse of a card already counted adds nothing to it
    public static bool WouldNewlyEnchant(CardModel card) => CanInfuse(card) && !EnchantedThisCombat.Contains(card);

    // A card is infusable if it takes the Ethereal keyword. It is also infusable if the enchantment for
    // its type stacks cleanly, that is the card has no enchantment or already has the same one. A second
    // Infuse on the same card increases the amount
    public static bool CanInfuse(CardModel card)
    {
        if (card.Type is CardType.Curse or CardType.Status or CardType.Quest)
            return !card.Keywords.Contains(CardKeyword.Ethereal);
        if (EnchantTypeFor(card) is not { } type) return false;
        return card.Enchantment == null || card.Enchantment.GetType() == type;
    }

    // Glow the Infuse selection gold for cards that gain an effect from being Enchanted, so the player sees
    // the best targets. CanInfuse keeps it to cards that can actually be picked. Hand selection only, because
    // CardSelectorPrefs.ShouldGlowGold applies to the hand
    private static bool ShouldGlowInfuse(CardModel card) =>
        AlchemistModConfig.ShowHandGlows && card is AlchemistCard { GainsEffectWhenEnchanted: true } && CanInfuse(card);

    // True when a fixed-count hand selection (min == max, so no manual confirmation) resolves with no screen,
    // because no more cards match the filter than the count. The player then sees no screen, so the caller
    // previews the result to make the automatic pick visible. This is the base game behavior for an upgraded
    // card from Armaments, where CardCmd.Upgrade previews the chosen card even when the pick auto-resolves
    internal static bool HandSelectIsAutomatic(Player owner, Func<CardModel, bool> filter, int min, int max)
    {
        if (min != max) return false;
        var matches = PileType.Hand.GetPile(owner).Cards.Count(filter);
        return matches > 0 && matches <= min;
    }

    public static Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile, int count) =>
        InfuseChosen(ctx, source, pile, count, count);

    // For a non-card source, for example a potion. Hand only, because a potion has no pile of its own
    public static async Task InfuseChosenFromHand(PlayerChoiceContext ctx, AbstractModel source, Player owner,
        int min, int max)
    {
        var prompt = max >= AlchemistCard.AnyNumber ? SelectPromptAny
            : min == max ? SelectPrompt
            : SelectPromptRange;
        var autoResolved = HandSelectIsAutomatic(owner, CanInfuse, min, max);
        var prefs = new CardSelectorPrefs(prompt, min, max) { ShouldGlowGold = ShouldGlowInfuse };
        var picks = (await CardSelectCmd.FromHand(ctx, owner, prefs, CanInfuse, source)).ToList();
        foreach (var card in picks)
            Infuse(card);
        // No screen was shown, so preview the infused cards to make the automatic infuse visible
        if (autoResolved && picks.Count > 0)
            CardCmd.Preview(picks);
    }

    // min/max lets the player choose how many to infuse: "up to N" (0..N) or "any number"
    // (0..AlchemistCard.AnyNumber)
    public static async Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile,
        int min, int max)
    {
        var prompt = max >= AlchemistCard.AnyNumber ? SelectPromptAny
            : min == max ? SelectPrompt
            : SelectPromptRange;
        // An automatic hand infuse shows no screen, so it needs a preview too, the same as a hidden pile
        var autoResolved = pile == PileType.Hand && HandSelectIsAutomatic(source.Owner, CanInfuse, min, max);
        var prefs = new CardSelectorPrefs(prompt, min, max) { ShouldGlowGold = ShouldGlowInfuse };
        var picks = (pile == PileType.Hand
            ? await CardSelectCmd.FromHand(ctx, source.Owner, prefs, CanInfuse, source)
            : await CardSelectCmd.FromCombatPile(ctx, pile.GetPile(source.Owner), source.Owner, prefs, CanInfuse))
            .ToList();
        foreach (var card in picks)
            Infuse(card);
        // Draw and discard picks are off screen. A hand pick is on screen, but an automatic one showed no
        // selection screen, so preview it too. A manual hand pick already showed the player the card
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
                TryEnchant<Fuming>(card, FumingAmount);
                break;
            case CardType.Power:
                TryEnchant<Exalted>(card, ExaltedAmount);
                break;
        }
    }

    // Enchant adds `amount` to the enchantment. It stacks if the card already has the same one
    private static void TryEnchant<T>(CardModel card, int amount) where T : EnchantmentModel
    {
        if (!ModelDb.Enchantment<T>().CanEnchant(card)) return;
        CardCmd.Enchant<T>(card, amount);
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
