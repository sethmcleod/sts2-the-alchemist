using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class AnodynePower : AlchemistPower
{
    /// <summary>HP prevented per point of Poison charged for it.</summary>
    public const int DamagePerDose = 4;

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
        Creature? dealer, CardModel? cardSource) =>
        Prevent(target, amount, props, dealer);

    private decimal Prevent(Creature target, decimal amount, ValueProp props, Creature? dealer)
    {
        _pendingPrevented = 0;
        if (amount <= 0 || !IsEnemyAttack(target, props, dealer)) return amount;

        _pendingPrevented = (int)amount;
        return 0m;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var prevented = _pendingPrevented;
        _pendingPrevented = 0;
        if (target != Owner || prevented <= 0) return;

        Flash();
        await PowerCmd.Remove(this);

        var dose = prevented / DamagePerDose;
        if (dose > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner, dose, Owner, null);
    }
}
