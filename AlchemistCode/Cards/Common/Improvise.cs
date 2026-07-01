using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Improvise : AlchemistCard
{
    public Improvise() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (CombatState == null) return;
        var discardPile = PileType.Discard.GetPile(Owner);
        var skills = discardPile.Cards.Where(c => c.Type == CardType.Skill).ToList();
        if (skills.Count > 0)
        {
            var randomSkill = skills[Owner.RunState.Rng.CombatCardGeneration.NextInt(skills.Count)];
            await CardPileCmd.Add(randomSkill, PileType.Hand);
            await CardCmd.AutoPlay(choiceContext, randomSkill, null);
        }
    }
}
