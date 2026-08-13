using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Alchemist.AlchemistCode.Compat;

namespace Alchemist.AlchemistCode.Powers;

// Turns the Alchemist's own self-poisoning into offence. Stateless on purpose: the amount is the
// Poison that was just gained, so nothing has to survive a mid-combat save
public class ShareThePainPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // An enemy power could answer our damage by poisoning us again, which would re-enter this hook
    private bool _resolving;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_resolving || amount <= 0m || power is not PoisonPower || power.Owner != Owner) return;
        Flash();
        _resolving = true;
        try
        {
            // Snapshot so a kill mid-sequence is respected. Unpowered keeps this out of the attack
            // pipeline: the damage is the Poison amount, not a hit that Strength should scale
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
                await GameCompat.Damage(new ThrowingPlayerChoiceContext(), enemy, amount,
                    ValueProp.Unpowered, Owner, null, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
