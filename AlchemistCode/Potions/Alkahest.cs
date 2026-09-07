using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Potions;

public class Alkahest : AlchemistPotion, IBrewOnly
{
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = target?.Player ?? Owner;
        // The base Apotheosis set: every combat pile, so the whole deck for the rest of combat
        if (player.PlayerCombatState is not { } combat) return Task.CompletedTask;
        var cards = combat.AllCards.Where(c => c.IsUpgradable).ToList();
        foreach (var card in cards)
            CardCmd.Upgrade(card);
        // Preview only what is visible: a row of the whole deck runs off the screen
        var hand = PileType.Hand.GetPile(player).Cards;
        var shown = cards.Where(hand.Contains).ToList();
        if (shown.Count > 0) CardCmd.Preview(shown);
        return Task.CompletedTask;
    }
}
