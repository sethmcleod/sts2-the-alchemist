using System.Linq;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Ancient;

public class Wormwood : AlchemistCard
{
    public Wormwood() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        WithDamage(3, 1);
        WithPower<PoisonPower>(2, 1);
        WithPower<AntitoxinPower>(2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy,
                DynamicVars.Poison.BaseValue, Owner.Creature, this);
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this);
    }
}
