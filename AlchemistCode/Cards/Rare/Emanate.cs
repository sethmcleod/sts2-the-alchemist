using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Emanate : AlchemistCard
{
    public Emanate() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithCalculatedDamage(6, 1, (card, _) =>
            card.Owner.Creature.GetPowerAmount<RegenPower>(), ValueProp.Move, 2, 0);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }
}
