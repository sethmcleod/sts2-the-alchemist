using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Corrode : AlchemistCard
{
    public Corrode() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Dose", Dose(this) is var p and > 0 ? $" ([green]{p}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var dose = (int)Dose(this);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            if (dose > 0)
            {
                PoisonSplash(enemy);
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, dose, Owner.Creature, this);
            }
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
    }
}
