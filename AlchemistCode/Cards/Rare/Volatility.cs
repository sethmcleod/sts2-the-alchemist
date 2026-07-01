using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Volatility : AlchemistCard
{
    public Volatility() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("poison", 3, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<VolatilityPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
    }
}
