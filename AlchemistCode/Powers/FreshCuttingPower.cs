using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class FreshCuttingPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromCard<FumingMix>(),
        HoverTipFactory.FromCard<AcridMix>(),
        HoverTipFactory.FromCard<SparklingMix>(),
    };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await Mixing.CreateRandom(choiceContext, player, kinds: Mixing.Special, source: this);
        await PowerCmd.Decrement(this);
    }
}
