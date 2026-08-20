using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class SnakeTail : AlchemistRelic
{
    private const int Antitoxin = 8;

    private bool _regrown;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>() };

    public override Task BeforeCombatStart()
    {
        _regrown = false;
        return Task.CompletedTask;
    }

    // _regrown is set before the grant, so the grant's own amount-changed event cannot re-enter.
    // The owner check matters in multiplayer, where this hook sees every creature's powers
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext,
        PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_regrown || power is not AntitoxinPower || power.Owner != Owner.Creature) return;
        if (amount >= 0 || Owner.Creature.GetPowerAmount<AntitoxinPower>() > 0) return;
        _regrown = true;
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            Antitoxin, Owner.Creature, null);
    }
}
