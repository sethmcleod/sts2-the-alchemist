using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Virulence : TheAlchemistCard
{
    public Virulence() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("extraPoison", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<VirulencePower>(choiceContext, Owner.Creature,
            DynamicVars["extraPoison"].IntValue, Owner.Creature, this);
    }
}
