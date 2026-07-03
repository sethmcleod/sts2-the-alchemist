using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Conjunction : AlchemistCard
{
    public Conjunction() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // WithEnergy creates an EnergyVar (required by the {Energy:energyIcons()} formatter — a plain
        // WithVar produces a generic DynamicVar that the formatter rejects, breaking the icon).
        WithEnergy(1, 1); // energy per turn: 1 -> 2 upgraded
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ConjunctionPower>(choiceContext, Owner.Creature,
            DynamicVars["Energy"].IntValue, Owner.Creature, this);
    }
}
