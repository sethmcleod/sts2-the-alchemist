using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Detonation : TheAlchemistCard
{
    public Detonation() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("damage", 5, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<DetonationPower>(choiceContext, Owner.Creature,
            DynamicVars["damage"].IntValue, Owner.Creature, this);
    }
}
