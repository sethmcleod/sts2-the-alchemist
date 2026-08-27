using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// Pays on the dose itself, not on what the dose did: 1 capacity per gain EVENT, whatever its
// size, so heavy doses do not snowball it. Reads the stack owner, not the applier, so poison
// an enemy puts on you still counts as a dose taken
public class PanaceaPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not PoisonPower || amount <= 0 || power.Owner != Owner) return;
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner, 1, Owner, null);
    }
}
