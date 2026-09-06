using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Potions;

public class Alkahest : AlchemistPotion, IBrewOnly
{
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.AnyTime;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (CombatManager.Instance.IsInProgress)
        {
            var player = target?.Player ?? Owner;
            // The base Apotheosis set: every combat pile, so the whole deck for the rest of combat
            var cards = player.PlayerCombatState.AllCards.Where(c => c.IsUpgradable).ToList();
            foreach (var card in cards)
                CardCmd.Upgrade(card);
            // Preview only what is visible: a row of the whole deck runs off the screen
            var shown = cards.Where(c => PileType.Hand.GetPile(player).Cards.Contains(c)).ToList();
            if (shown.Count > 0) CardCmd.Preview(shown);
            return;
        }

        var chosen = (await CardSelectCmd.FromDeckForUpgrade(Owner,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (chosen != null)
            CardCmd.Upgrade(chosen);
    }
}
