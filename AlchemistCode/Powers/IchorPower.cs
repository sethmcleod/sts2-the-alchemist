using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// Amount is the turn-start dose. The damage bonus reads the live Poison, so it is not on the icon
public partial class IchorPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner, Amount, Owner, null);
    }

    // The damage hook is spelled differently on the two game branches, so the override lives in
    // Compat/DoseReaderPowerCompat.cs and calls this. The same guard Laced uses: powered card attacks
    // only, never a card's incidental damage
    internal decimal DoseBonus(ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && cardSource != null && props.IsPoweredAttack()
            ? Owner.GetPowerAmount<PoisonPower>()
            : 0m;
}
