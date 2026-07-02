using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Osmosis : AlchemistCard
{
    protected override bool HasEnergyCostX => true;

    public Osmosis() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var x = ResolveEnergyXValue();
        var drawCount = x + (IsUpgraded ? 1 : 0);
        var poisonAmount = x + 1;
        if (drawCount > 0)
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        if (poisonAmount > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, poisonAmount, Owner.Creature, this);
        if (IsUpgraded && x + 1 > 0)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, x + 1, Owner.Creature, this);
    }
}
