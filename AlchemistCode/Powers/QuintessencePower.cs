using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class QuintessencePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    internal bool Plus;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef }.Concat(Mixing.MixTips(Plus));

    internal async Task OnFermented(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player == null) return;
        Flash();
        for (var i = 0; i < Amount; i++)
            await Mixing.CreateRandom(choiceContext, Owner.Player, Plus, source: this);
    }
}
