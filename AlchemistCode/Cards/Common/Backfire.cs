using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.None)]
public class Backfire : AlchemistCard
{
    private const int VersusAttacker = 4;

    public Backfire() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(14, static (_, target) => IntendsToAttack(target) ? VersusAttacker : 0m,
            ValueProp.Move, 6);
    }

    private static bool IntendsToAttack(Creature? target) => target?.Monster is { IntendsToAttack: true };

    protected override bool ConditionalGlow =>
        IsMutable && CombatState?.Enemies.Where(e => e.IsAlive).ToList() is { Count: > 0 } enemies
        && enemies.TrueForAll(IntendsToAttack);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
