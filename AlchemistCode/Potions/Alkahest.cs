using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Potions;

public class Alkahest : AlchemistPotion, IBrewOnly
{
    // Event rarity keeps this out of every rarity-filtered roll and files it under "Special" in the
    // potion lab, which is where a potion that is "created by other means" belongs
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.AnyTime;
    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips => Infusion.InfuseTips();

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (CombatManager.Instance.IsInProgress)
        {
            await Infusion.InfuseChosenFromHand(choiceContext, this, Owner, 0, 3);
            return;
        }
        var card = (await CardSelectCmd.FromDeckForUpgrade(Owner,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
            CardCmd.Upgrade(card);
    }
}
