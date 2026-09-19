using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Under never-spent capacity a dose that is not above the Antitoxin deals nothing, so the test is
// the plain comparison rather than the tick bookkeeping, and it reads the same before or after
// the Poison tick of the turn
public class UnfazedPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        var poison = Owner.GetPowerAmount<PoisonPower>();
        if (poison <= 0 || poison > Owner.GetPowerAmount<AntitoxinPower>()) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, (int)Amount, player);
    }
}
