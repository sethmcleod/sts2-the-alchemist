using MegaCrit.Sts2.Core.Commands;
using System.Linq;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class ChainReaction : AlchemistCard
{
    public ChainReaction() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(12, 4);
        WithPower<PoisonPower>(4, 2);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay).Execute(choiceContext);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy,
                DynamicVars.Poison.BaseValue, Owner.Creature, this);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            await PoisonTrigger.Once(choiceContext, enemy);
    }
}
