using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Inversion : AlchemistCard
{
    public Inversion() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Percent", 50, 50);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<InversionPower>(choiceContext, Owner.Creature,
            DynamicVars["Percent"].IntValue, Owner.Creature, this);
    }
}
