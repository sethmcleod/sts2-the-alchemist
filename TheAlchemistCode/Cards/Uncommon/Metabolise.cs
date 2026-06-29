using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Metabolise : TheAlchemistCard
{
    public Metabolise() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithTip(typeof(PoisonPower));
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        var amount = poison + (IsUpgraded ? 2 : 0);
        if (amount > 0)
            await PowerCmd.Apply<MetaboliseStrengthPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }
}
