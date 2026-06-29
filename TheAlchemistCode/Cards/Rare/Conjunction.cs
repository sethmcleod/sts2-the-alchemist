using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Conjunction : TheAlchemistCard
{
    public Conjunction() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ConjunctionPower>(choiceContext, Owner.Creature,
            IsUpgraded ? 2 : 1, Owner.Creature, this);
    }
}
