using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Invigorate : AlchemistCard
{
    public Invigorate() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(8, 1, (card, _) =>
            card.Owner.Creature.GetPowerAmount<RegenPower>(), ValueProp.Move, 2, 0);
        WithPower<RegenPower>(2, 0); // flat 2 Regen, gained after the attack
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Attack first so the bonus damage uses your current Regen (matches the card preview),
        // then gain the 2 Regen as a rider for future turns.
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
