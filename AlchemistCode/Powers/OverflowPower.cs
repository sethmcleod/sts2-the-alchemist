using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class OverflowPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef, HoverTipFactory.FromPower<PoisonPower>() };

    internal async Task OnFermented(PlayerChoiceContext choiceContext, AlchemistCard card, int turns)
    {
        if (turns <= 0) return;
        var upgrading = card.IsUpgradable;
        if (!upgrading && !card.IsUpgraded) return;
        Flash();
        if (upgrading)
        {
            CardCmd.Upgrade(card);
            CardCmd.Preview(card);
            turns--;
        }
        if (turns == 0 || CombatState == null) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, CombatState.HittableEnemies,
            Amount * turns, Owner, null);
    }
}
