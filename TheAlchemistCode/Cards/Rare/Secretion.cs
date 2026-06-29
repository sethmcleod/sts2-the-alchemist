using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Secretion : TheAlchemistCard
{
    public Secretion() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("poison", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SecretionPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
    }
}
