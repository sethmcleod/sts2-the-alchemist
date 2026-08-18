using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

// The Rare hub engine: every Attack becomes a reader, and the dose that feeds it drips in on its own
[CardTheme(CardTheme.Poison)]
public class Ichor : AlchemistCard
{
    public Ichor() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("SelfPoison", 2, 0);
        WithCostUpgradeBy(-1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<IchorPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
    }
}
