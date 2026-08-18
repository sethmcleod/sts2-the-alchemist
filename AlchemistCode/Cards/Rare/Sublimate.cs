using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

// The passive reader: while you carry a dose it hits the biggest enemy every turn on its own
[CardTheme(CardTheme.Poison)]
public class Sublimate : AlchemistCard
{
    public Sublimate() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithVar("Multiplier", 2, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SublimatePower>(choiceContext, Owner.Creature,
            DynamicVars["Multiplier"].IntValue, Owner.Creature, this);
    }
}
