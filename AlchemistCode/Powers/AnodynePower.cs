using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// Base BufferPower's shape: zero the damage in the modify hook, then settle up in
// AfterModifyingHpLostBeforeOsty, which the pipeline calls synchronously on exactly the models
// whose modify hook changed the amount. The earlier design waited for AfterDamageReceived, which
// never reached this power in multiplayer, so the shield neither expired nor charged its Poison
// and the drinker was immortal
public class AnodynePower : AlchemistPower
{
    /// <summary>HP prevented per point of Poison charged for it.</summary>
    public const int DamagePerDose = 5;

    private int _pendingPrevented;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // Block is subtracted before this hook, so armouring up genuinely lowers the Poison bill
    private bool IsEnemyAttack(Creature target, ValueProp props, Creature? dealer) =>
        target == Owner && dealer != null && dealer != Owner && !dealer.IsPlayer
        && props.HasFlag(ValueProp.Move);

    // Verified identical on both game branches, unlike AntitoxinPower's ModifyDamageAdditive, so this
    // stays inline rather than in Compat/
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (amount <= 0 || !IsEnemyAttack(target, props, dealer)) return amount;
        _pendingPrevented = (int)amount;
        return 0m;
    }

    public override async Task AfterModifyingHpLostBeforeOsty()
    {
        var prevented = _pendingPrevented;
        _pendingPrevented = 0;
        if (prevented <= 0) return;

        Flash();
        await PowerCmd.Remove(this);

        var dose = prevented / DamagePerDose;
        if (dose > 0)
            await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), Owner, dose, Owner, null);
    }
}
