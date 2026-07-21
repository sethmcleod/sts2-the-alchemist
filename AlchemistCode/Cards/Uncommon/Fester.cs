using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Fester : AlchemistCard
{
    public Fester() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<PoisonPower>(3, 0);
        WithVar("triggers", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, play.Target,
            DynamicVars.Poison.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<FesterPower>(choiceContext, play.Target,
            DynamicVars["triggers"].IntValue, Owner.Creature, this);
    }
}
