using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Rectify : TheAlchemistCard
{
    public Rectify() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(10, 4);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poison > 0)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, poison, Owner.Creature, this);
    }
}
