using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Fallout : AlchemistCard
{
    public Fallout() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(5, 2);
        WithPower<PoisonPower>(3, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy,
                DynamicVars.Poison.IntValue, Owner.Creature, this);
    }
}
