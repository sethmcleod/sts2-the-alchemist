using System.Linq;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class WaterDown : AlchemistCard
{
    public WaterDown() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithVar("StrengthLoss", 3, 2);
        WithVar("Poison", 3, 1);
        WithTip(typeof(StrengthPower));
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            await PowerCmd.Apply<WaterDownPower>(choiceContext, enemy,
                DynamicVars["StrengthLoss"].IntValue, Owner.Creature, this);
            PoisonSplash(enemy);
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy,
                DynamicVars["Poison"].IntValue, Owner.Creature, this);
        }
    }
}
