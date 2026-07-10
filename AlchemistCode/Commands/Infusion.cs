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
    // Corrupted and Glam ignore Amount; only Sown uses it (energy gained the first time you play the card)
    private const int SownEnergy = 1;

    private static readonly LocString SelectPrompt = new("card_keywords", "ALCHEMIST-INFUSE.selectionPrompt");

    private static readonly HashSet<CardModel> Infused = new();

    // Curses get Ethereal as a keyword, and clearing an enchantment never undoes a keyword — so remember only
    // the cards we added it to, and strip it at combat end
    private static readonly HashSet<CardModel> AddedEthereal = new();

    // Distinct cards enchanted (by any source) during the current combat. Recorded from the shared
    // CardCmd.Enchant hook so Masterwork's threshold counts other mods' enchantments too, not just Infuse
    private static readonly HashSet<CardModel> EnchantedThisCombat = new();

    public static void RecordCombatEnchant(CardModel card) => EnchantedThisCombat.Add(card);

    public static int EnchantedThisCombatCount(Player owner) => EnchantedThisCombat.Count(c => c.Owner == owner);

    // Infusions don't stack, so an already-enchanted or already-infused card is never offered or picked
    public static bool CanInfuse(CardModel card) => card.Enchantment == null && !Infused.Contains(card);

    public static async Task InfuseChosen(PlayerChoiceContext ctx, AlchemistCard source, PileType pile, int count)
    {
        var prefs = new CardSelectorPrefs(SelectPrompt, count);
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

    public static void InfuseAllInHand(AlchemistCard source)
    {
        foreach (var card in PileType.Hand.GetPile(source.Owner).Cards
                     .Where(c => c != source && CanInfuse(c)).ToList())
            Infuse(card);
    }

    public static void InfuseRandomFromPile(Player owner, PileType pile, int count)
    {
        var rng = owner.RunState.Rng.CombatCardGeneration;
        var cards = pile.GetPile(owner).Cards.Where(CanInfuse).ToList();
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
            if (!card.Keywords.Contains(CardKeyword.Ethereal))
            {
                card.AddKeyword(CardKeyword.Ethereal);
                AddedEthereal.Add(card);
            }
            Infused.Add(card);
            return;
        }

        switch (card.Type)
        {
            case CardType.Attack:
                TryEnchant<Corrupted>(card, 1);
                break;
            case CardType.Skill:
                TryEnchant<Glam>(card, 1);
                break;
            case CardType.Power:
                TryEnchant<Sown>(card, SownEnergy);
                break;
        }
    }

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
