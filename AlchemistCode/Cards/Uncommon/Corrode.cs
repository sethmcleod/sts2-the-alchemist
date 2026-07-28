using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Corrode : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Skill;

    public Corrode() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<PoisonPower>(6, 0);
        WithPower<WeakPower>(1, 1);
        WithVar("ReactionPoison", 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var poison = DynamicVars.Poison.IntValue
                     + (ReactionActive ? DynamicVars["ReactionPoison"].IntValue : 0);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, poison, Owner.Creature, this);
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
    }
}
