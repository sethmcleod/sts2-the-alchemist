using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class ShedSkin : TheAlchemistCard
{
    public ShedSkin() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(7, 1, (card, _) =>
            card.Owner.Creature.HasPower<PlatingPower>()
                ? card.Owner.Creature.GetPowerAmount<PlatingPower>() * 2
                : 0,
            ValueProp.Move, 4, 0);
        WithTip(typeof(PlatingPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (Owner.Creature.HasPower<PlatingPower>())
            await PowerCmd.Remove<PlatingPower>(Owner.Creature);
    }
}
