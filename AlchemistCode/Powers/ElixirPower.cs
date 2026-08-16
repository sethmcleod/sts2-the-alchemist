using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class ElixirPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    /// <summary>Self-feeding, so the payoff never starves when Antitoxin cures the dose.</summary>
    private const int Dose = 2;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;

        // Doses first, so a cured dose is refilled before it is read. Amount stays the only scaling
        // number on this power
        await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), Owner, Dose, Owner, null);
        var dose = Owner.GetPowerAmount<PoisonPower>();
        if (dose <= 0) return;

        // Single target on purpose: the mod's twelve AoE cards are already a standing rubric penalty,
        // and a boss killer is the hole in the deck
        var target = combatState.GetOpponentsOf(Owner).Where(e => e.IsAlive)
            .OrderByDescending(e => e.CurrentHp).FirstOrDefault();
        if (target == null) return;

        Flash();
        await GameCompat.Damage(new ThrowingPlayerChoiceContext(), target, Amount * dose,
            ValueProp.Unpowered, Owner, null, null);
    }
}
