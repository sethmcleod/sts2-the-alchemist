using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class GoldenTouch : AlchemistCard
{
    public GoldenTouch() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        // Only the upgrade Infuses, so the tips are only shown there
        WithTips(card => card.IsUpgraded ? Infusion.InfuseTips() : Array.Empty<IHoverTip>());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<GoldenTouchPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (IsUpgraded)
            await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
    }
}
