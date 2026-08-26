using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Corrode : AlchemistCard
{
    public Corrode() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(1, 1);
        WithTip(StaticHoverTip.Block);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            if (enemy.Block > 0)
                await GameCompat.LoseBlock(choiceContext, enemy, enemy.Block, Owner.Creature);
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
    }
}
