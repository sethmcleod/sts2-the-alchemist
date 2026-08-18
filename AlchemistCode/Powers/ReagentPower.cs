using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// Every powered card attack gains the dose this turn. Readers already add it once themselves, so on
// them this reads as counting the dose twice; on a plain Strike it is the dose once
public partial class ReagentPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // The damage hook is spelled differently on the two game branches, so the override lives in
    // Compat/DoseReaderPowerCompat.cs and calls this
    internal decimal DoseBonus(ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && cardSource != null && props.IsPoweredAttack()
            ? Owner.GetPowerAmount<PoisonPower>()
            : 0m;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
