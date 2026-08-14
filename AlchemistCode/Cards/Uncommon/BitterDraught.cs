using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class BitterDraught : AlchemistCard
{
    public BitterDraught() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithEnergy(3, 0);
        WithPower<PoisonPower>(2, 0);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
        // After the gain, so the tick is the whole stack. It carries the real Poison tick shape, so
        // Antitoxin absorbs it and every absorb payoff fires
        await PoisonTrigger.Once(choiceContext, Owner.Creature);
    }
}
