using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Inversion : TheAlchemistCard
{
    public Inversion() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Amount=1: random enemy. Amount=2: ALL enemies.
        var amount = IsUpgraded ? 2 : 1;
        await PowerCmd.Apply<InversionPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }
}
