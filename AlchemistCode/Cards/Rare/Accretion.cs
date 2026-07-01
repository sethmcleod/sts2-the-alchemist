using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Accretion : AlchemistCard
{
    public Accretion() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("plating", 2, 1);
        WithTip(typeof(PlatingPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AccretionPower>(choiceContext, Owner.Creature,
            DynamicVars["plating"].IntValue, Owner.Creature, this);
    }
}
