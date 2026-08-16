using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Commands;

// One tick of Poison, landing exactly the way PoisonPower.Trigger lands its own: the amount read off
// the power, unblockable and unpowered, with no dealer and no card source, and a decrement only while
// the creature still lives.
//
// Each of those matters in multiplayer. CreatureCmd.Damage returns early when the dealer is dead, so a
// trigger that names the Alchemist as dealer silently deals nothing the moment the Alchemist dies, and
// whether that has happened yet can differ between clients. Naming a card source hides the tick from
// every power that reads for a real one: Antitoxin, Callus, Grudge, Warded and PassItOn.
// Applying -1 instead of decrementing re-runs the apply hooks, so Heavy Hand and friends fire on a
// removal
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
