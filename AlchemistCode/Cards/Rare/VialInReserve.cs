using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Potions)]
public class VialInReserve : AlchemistCard
{
    public VialInReserve() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<VialInReservePower>(choiceContext, Owner.Creature,
            DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
