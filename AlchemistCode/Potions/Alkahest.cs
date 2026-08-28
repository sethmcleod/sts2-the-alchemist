using System.Linq;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;

namespace Alchemist.AlchemistCode.Potions;

public class Alkahest : AlchemistPotion, IBrewOnly
{
    private static readonly PileType[] Piles = { PileType.Hand, PileType.Draw, PileType.Discard };

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.AnyTime;
    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Decant) };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (CombatManager.Instance.IsInProgress)
        {
            var player = target?.Player ?? Owner;
            foreach (var pileType in Piles)
            {
                foreach (var card in pileType.GetPile(player).Cards)
                {
                    if (card is AlchemistCard { IsDecantCard: true } decantCard)
                        decantCard.AddDecant(decantCard.DecantMaxValue);
                }
            }
            return;
        }
        var chosen = (await CardSelectCmd.FromDeckForUpgrade(Owner,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (chosen != null)
            CardCmd.Upgrade(chosen);
    }
}
