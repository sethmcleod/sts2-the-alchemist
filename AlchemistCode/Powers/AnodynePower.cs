using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// Settles in AfterModifyingHpLostBeforeOsty like BufferPower. AfterDamageReceived never reaches it in multiplayer
public class AnodynePower : AlchemistPower
{
    public const int DamagePerDose = 10;

    private int _pendingPrevented;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    private bool IsEnemyAttack(Creature target, ValueProp props, Creature? dealer) =>
        target == Owner && dealer != null && dealer != Owner && !dealer.IsPlayer
        && props.HasFlag(ValueProp.Move);

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
