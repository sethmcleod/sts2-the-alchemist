using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Spatter : AlchemistCard
{
    public Spatter() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(6, 3);
        WithVar("Poison", 3, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        var dose = Math.Min(DynamicVars["Poison"].IntValue, (int)Dose(this));
        if (dose <= 0 || CombatState == null) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -dose, Owner.Creature, this);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            PoisonSplash(enemy);
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, dose, Owner.Creature, this);
        }
    }
}
