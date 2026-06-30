using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Relics;

public class CinnabarShard : TheAlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    private bool _triggering;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_triggering) return;
        if (result.UnblockedDamage <= 0) return;
        if (dealer != null || cardSource != null) return;
        if (!props.HasFlag(ValueProp.Unblockable) || !props.HasFlag(ValueProp.Unpowered)) return;
        if (target == Owner.Creature) return;

        _triggering = true;
        Flash();
        await CreatureCmd.Damage(choiceContext, target, result.UnblockedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        _triggering = false;
    }
}
