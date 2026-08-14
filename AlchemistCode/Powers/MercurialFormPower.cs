using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class MercurialFormPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _granted;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[]
        {
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<PoisonPower>(),
        };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.GetPower<PoisonPower>() is not { Amount: > 0 } poison) return;

        var dose = poison.Amount;
        Flash();
        // The hit lands before the stack is removed, so it carries the Poison tick shape that Antitoxin
        // and every absorb payoff read. Removing first would make it an ordinary unblockable hit
        await GameCompat.Damage(choiceContext, Owner, dose,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        if (!Owner.IsAlive) return;

        await PowerCmd.Remove(poison);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, dose, Owner, null);
        _granted += dose;
    }

    // Plain StrengthPower rather than a TemporaryStrengthPower subclass, which would need its own icon.
    // The card text states the loss instead, the way Flex Potion does
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || _granted <= 0) return;
        var owed = _granted;
        _granted = 0;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -owed, Owner, null);
    }
}
