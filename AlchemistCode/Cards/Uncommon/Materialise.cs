using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Materialise : AlchemistCard
{
    public Materialise() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("blockGain", 2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<MaterialisePower>(choiceContext, Owner.Creature,
            DynamicVars["blockGain"].IntValue, Owner.Creature, this);
    }
}
