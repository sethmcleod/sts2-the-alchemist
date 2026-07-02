using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Powers;

// Marker power, like the base game's Accelerant. FesterPoisonTriggerPatch folds this power's Amount
// into PoisonPower.TriggerCount on the owner, so the game's own Poison logic drives the extra ticks
// (each deals the current Poison then decrements it) AND the lethal-HP prediction — that's why an
// enemy that Fester will kill shows green HP. One-shot: removed at the end of the owner's next turn.
public class FesterPower : AlchemistPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // The poison tick already fired at this turn's start; Fester is spent, so expire it.
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
