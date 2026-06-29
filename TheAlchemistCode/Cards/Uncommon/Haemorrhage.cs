using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Haemorrhage : TheAlchemistCard
{
    public Haemorrhage() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCostUpgradeBy(-1);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (regen > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature,
                regen, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        var multiplier = IsUpgraded ? 3 : 2;
        var damage = regen * multiplier;
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this)
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
