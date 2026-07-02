using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// Whenever the owner takes Poison damage, apply that much Poison (x Amount, so copies stack)
// to ALL enemies. Poison-tick detection matches ChrysopoeiaPower: unblockable+unpowered
// damage with no dealer or card source.
public class InducementPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0) return;
        if (dealer != null || cardSource != null) return;
        if (!props.HasFlag(ValueProp.Unblockable) || !props.HasFlag(ValueProp.Unpowered)) return;

        Flash();
        var poison = result.UnblockedDamage * Amount;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, poison, Owner, null);
    }
}
