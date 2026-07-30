using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Slag : AlchemistCard
{
    public Slag() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Block", 2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SlagPower>(choiceContext, Owner.Creature,
            DynamicVars["Block"].IntValue, Owner.Creature, this);
    }
}
