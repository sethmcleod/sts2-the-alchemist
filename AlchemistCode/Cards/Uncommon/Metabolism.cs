using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Metabolism : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Metabolism() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithEnergy(1, 0);
        WithVar("heal", 3, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<MetabolismPower>(choiceContext, Owner.Creature,
            DynamicVars.Energy.BaseValue, Owner.Creature, this);
        if (IsReduced)
            await CreatureCmd.Heal(Owner.Creature, DynamicVars["heal"].IntValue);
    }
}
