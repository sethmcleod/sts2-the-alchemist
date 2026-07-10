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

// Infuse: enchant a card until end of combat. Enchantments are run-permanent by default, so infused
// cards are tracked here and cleared at combat end by Patches.InfusionCombatEndPatch
public static class Infusion
{
    private const int Amount = 3;      // Attacks (Sharp) and Skills (Nimble/Adroit)
    private const int PowerAmount = 2; // Powers (Swift)
    private static readonly LocString SelectPrompt = new("card_keywords", "ALCHEMIST-INFUSE.selectionPrompt");

    private static readonly HashSet<CardModel> Infused = new();

    // Distinct cards enchanted (by any source) during the current combat. Recorded from the shared
    // CardCmd.Enchant hook so Masterwork's threshold counts other mods' enchantments too, not just Infuse
    private static readonly HashSet<CardModel> EnchantedThisCombat = new();

    public static void RecordCombatEnchant(CardModel card) => EnchantedThisCombat.Add(card);

    public static int EnchantedThisCombatCount(Player owner) => EnchantedThisCombat.Count(c => c.Owner == owner);

    public static async Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile, int count)
    {
        var prefs = new CardSelectorPrefs(SelectPrompt, count);
        var picks = (pile == PileType.Hand
            ? await CardSelectCmd.FromHand(ctx, source.Owner, prefs, null, source)
            : await CardSelectCmd.FromCombatPile(ctx, pile.GetPile(source.Owner), source.Owner, prefs)).ToList();
        foreach (var card in picks)
            Infuse(card);
        // Draw/discard picks aren't visible to the player, so pop them up; hand picks already are
        if (pile is PileType.Draw or PileType.Discard && picks.Count > 0)
            CardCmd.Preview(picks);
    }

    public static async Task InfuseAnyFromHand(PlayerChoiceContext ctx, AlchemistCard source)
    {
        var prefs = new CardSelectorPrefs(SelectPrompt, 0, 999999999);
        var picks = await CardSelectCmd.FromHand(ctx, source.Owner, prefs, null, source);
        foreach (var card in picks)
            Infuse(card);
    }

    public static void InfuseAllInHand(AlchemistCard source)
    {
        foreach (var card in PileType.Hand.GetPile(source.Owner).Cards.Where(c => c != source).ToList())
            Infuse(card);
    }

    public static void InfuseRandomFromPile(Player owner, PileType pile, int count)
    {
        var rng = owner.RunState.Rng.CombatCardGeneration;
        var cards = pile.GetPile(owner).Cards.ToList();
        var infused = new List<CardModel>();
        for (var i = 0; i < count && cards.Count > 0; i++)
        {
            var card = cards[rng.NextInt(cards.Count)];
            cards.Remove(card);
            Infuse(card);
            infused.Add(card);
        }
        if (infused.Count > 0)
            CardCmd.Preview(infused);
    }

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
        // Random picks aren't visible to the player, so pop them up
        if (infused.Count > 0)
            CardCmd.Preview(infused);
    }

    public static void Infuse(CardModel card)
    {
        if (card.Type is CardType.Curse or CardType.Status or CardType.Quest)
        {
            card.AddKeyword(CardKeyword.Ethereal);
            return;
        }

        switch (card.Type)
        {
            case CardType.Attack:
                TryEnchant<Sharp>(card, Amount);
                break;
            case CardType.Skill:
                // Nimble adds its bonus to EACH block gain (scales on multi-block skills); Adroit adds once
                if (card.GainsBlock) TryEnchant<Nimble>(card, Amount);
                else TryEnchant<Adroit>(card, Amount);
                break;
            case CardType.Power:
                TryEnchant<Swift>(card, PowerAmount);
                break;
        }
    }

    private static void TryEnchant<T>(CardModel card, int amount) where T : EnchantmentModel
    {
        // Base enchantments aren't IsStackable, so to stack a re-Infuse we clear and re-apply the summed
        // amount. Only stack onto our own infusions — never a pre-existing/permanent one
        if (card.Enchantment is T existing)
        {
            if (!Infused.Contains(card)) return;
            var summed = existing.Amount + amount;
            CardCmd.ClearEnchantment(card);
            CardCmd.Enchant<T>(card, summed);
            return;
        }
        if (card.Enchantment != null) return;                  // Different enchantment type — don't cross-stack
        if (!ModelDb.Enchantment<T>().CanEnchant(card)) return;
        CardCmd.Enchant<T>(card, amount);
        Infused.Add(card);
    }

    public static void ClearCombatInfusions()
    {
        foreach (var card in Infused)
            if (card.Enchantment != null)
                CardCmd.ClearEnchantment(card);
        Infused.Clear();
        EnchantedThisCombat.Clear();
    }
}
