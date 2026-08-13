using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Commands;

// A trigger has to land with the shape PoisonPower ticks with: unblockable and unpowered, with no
// dealer and no card. Antitoxin, Callus, Grudge, Warded, PassItOn and Contagion all read that
// shape, so a trigger that carries a dealer or a card source is invisible to every one of them
public static class PoisonTrigger
{
    public static async Task Once(PlayerChoiceContext ctx, Creature creature)
    {
        if (creature.GetPower<PoisonPower>() is not { Amount: > 0 } poison) return;
        await GameCompat.Damage(ctx, creature, poison.Amount,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        if (creature.IsAlive) await PowerCmd.Decrement(poison);
    }
}
