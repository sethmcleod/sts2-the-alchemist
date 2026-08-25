using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.None)]
public class SmokeOut : AlchemistCard
{
    public SmokeOut() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(6, 2);
        WithPower<WeakPower>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
        foreach (var enemy in CombatState.HittableEnemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy,
                DynamicVars.Weak.IntValue, Owner.Creature, this);
    }
}
