using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

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
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner, 1, Owner, null);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner, 1, Owner, null);
        var dose = Owner.GetPowerAmount<PoisonPower>();
        if (dose <= 0) return;
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
