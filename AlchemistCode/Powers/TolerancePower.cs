using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class TolerancePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>(), HoverTipFactory.FromPower<PoisonPower>() };

    // Poison per point of capacity granted
    public const int PoisonPerPoint = 3;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        var gain = Owner.GetPowerAmount<PoisonPower>() / PoisonPerPoint * Amount;
        if (gain <= 0) return;
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner, gain, Owner, null);
    }
}
