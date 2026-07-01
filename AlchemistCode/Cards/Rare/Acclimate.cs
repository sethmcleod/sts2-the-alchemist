using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Acclimate : AlchemistCard
{
    public Acclimate() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("plating", 1, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(PlatingPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AcclimatePower>(choiceContext, Owner.Creature,
            DynamicVars["plating"].IntValue, Owner.Creature, this);
    }
}
