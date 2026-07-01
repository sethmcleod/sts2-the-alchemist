using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Exude : AlchemistCard
{
    public Exude() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(3, 1, (card, _) =>
            card.Owner.Creature.GetPowerAmount<PoisonPower>()
            + (card.IsUpgraded ? card.Owner.Creature.GetPowerAmount<RegenPower>() : 0),
            ValueProp.Move, 0, 0);
        WithTip(typeof(PoisonPower));
        WithTips(card => card.IsUpgraded
            ? [HoverTipFactory.FromPower<RegenPower>()]
            : []);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }
}
