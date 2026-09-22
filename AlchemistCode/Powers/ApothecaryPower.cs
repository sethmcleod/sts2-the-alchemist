using System.Collections.Generic;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Alchemist.AlchemistCode.Powers;

public class ApothecaryPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Upgraded", 0) };

    private bool Upgraded => DynamicVars["Upgraded"].IntValue > 0;

    internal void MarkUpgraded() => DynamicVars["Upgraded"].BaseValue = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => Mixing.MixRefTips();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        for (var i = 0; i < Amount; i++)
            await Mixing.CreateRandom(choiceContext, Owner.Player!, Upgraded, source: this);
    }
}
