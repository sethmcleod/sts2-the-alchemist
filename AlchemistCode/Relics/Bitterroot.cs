using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class Bitterroot : AlchemistRelic
{
    private const int Heal = 3;
    private const int Dose = 1;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // Both land in the Late phase of the first player turn, the way FlaskRelic doses. PoisonPower
    // triggers and decrements on AfterSideTurnStart, so a dose applied before combat is eaten by the
    // turn-1 trigger before the player can act; landing here means the first tick comes on turn 2
    public override async Task AfterSideTurnStartLate(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (combatState.RoundNumber != 1 || !participants.Contains(Owner.Creature)) return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, Heal);
        await PowerCmd.Apply<PoisonPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, Dose, Owner.Creature, null);
    }
}
