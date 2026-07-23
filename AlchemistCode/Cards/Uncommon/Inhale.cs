using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Inhale : AlchemistCard
{
    public Inhale() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
        WithTips(_ => Infusion.InfuseTips());
        ExplainNumber("ALCHEMIST-INHALE");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var totalPoison = CombatState.Enemies
            .Concat(CombatState.PlayerCreatures)
            .Where(c => c.IsAlive)
            .Sum(c => c.GetPowerAmount<PoisonPower>());
        if (totalPoison > 0)
        {
            var times = IsUpgraded ? 2 : 1;
            for (var i = 0; i < times; i++)
                await CreatureCmd.GainBlock(Owner.Creature, totalPoison, ValueProp.Move, play);
        }
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
    }
}
