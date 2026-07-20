using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Catalyze : AlchemistCard
{
    public Catalyze() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Amount", 2, 0);
        WithCostUpgradeBy(-1);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<CatalyzePower>(choiceContext, Owner.Creature,
            DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
