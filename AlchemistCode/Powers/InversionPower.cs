using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// Whenever you are healed, deal that much damage to ALL enemies — base copies add one hit,
// upgraded copies add two. Amount is the total stack count (display); Hits is the summed
// trigger count, so mixed base+upgraded stacks behave as the sum of the copies.
public class InversionPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Hits", 0m) };

    /// <summary>Called by the card after each apply — upgraded copies hit twice per heal.</summary>
    public void RegisterCopy(bool upgraded) =>
        DynamicVars["Hits"].BaseValue += upgraded ? 2 : 1;

    // Reentrancy guard: our damage can trigger heals (e.g. lifesteal-style effects), whose
    // AfterCurrentHpChanged would re-enter this hook and loop unbounded.
    private bool _resolving;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta <= 0 || _resolving) return; // positive delta = healed
        var hits = DynamicVars["Hits"].IntValue;
        if (hits <= 0) return;
        Flash();
        _resolving = true;
        try
        {
            // Re-snapshot alive enemies per hit so kills mid-sequence are respected.
            for (var i = 0; i < hits; i++)
                foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
                    await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), enemy, delta, ValueProp.Move, Owner, null, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
