using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Character;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs and the two power compat files. An override cannot be
// routed through a wrapper, and here the whole body differs, so the method lives here rather than in
// Character/Alchemist.cs.
//
// THIS COPY IS THE beta IMPLEMENTATION. ON A MERGE INTO main, KEEP main's SIDE. main's copy can do
// none of the near death work below: its GenerateAnimator is handed no Creature, its CharacterModel
// has no IsLowHealth, and its AnimState has no AddNextState for a conditional fall-back
public partial class Alchemist
{
    // Overrides GenerateAnimator rather than BaseLib's SetupCustomAnimationStates, because only
    // this signature carries the Creature, and the low health idle needs it. The base game does not
    // raise a trigger for low health: it registers TWO Idle states with opposite conditions and lets
    // every one shot pick the idle that fits when it ends. IsLowHealth is a quarter HP or less
    public override CreatureAnimator GenerateAnimator(MegaSprite controller, Creature creature)
    {
        AlchemistVisuals.StartIdleFlourishes(controller);

        var idle = new AnimState(AlchemistVisuals.IdleAnimation, isLooping: true);
        var lowIdle = AlchemistVisuals.NearDeathAnimation is { } low
            ? new AnimState(low, isLooping: true)
            : idle;

        bool Low() => IsLowHealth(creature);

        AnimState Once(string? name)
        {
            if (name == null) return idle;

            var state = new AnimState(name);
            state.AddNextState(idle, () => !Low());
            state.AddNextState(lowIdle, Low);
            return state;
        }

        var hurt = Once(AlchemistVisuals.HurtAnimation);
        var attack = Once(AlchemistVisuals.AttackAnimation);
        var cast = Once(AlchemistVisuals.CastAnimation);
        var heavy = Once(AlchemistVisuals.HeavyAttackAnimation ?? AlchemistVisuals.AttackAnimation);
        var dead = AlchemistVisuals.DeathAnimation is { } death ? new AnimState(death) : idle;

        var relaxed = idle;
        if (AlchemistVisuals.RelaxedAnimation is { } relaxedName)
        {
            relaxed = new AnimState(relaxedName, isLooping: true);
            relaxed.AddBranch("Idle", idle);
        }

        var animator = new CreatureAnimator(Low() ? lowIdle : idle, controller);
        animator.AddAnyState("Idle", idle, () => !Low());
        animator.AddAnyState("Idle", lowIdle, Low);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("heavyAttack", heavy);
        animator.AddAnyState("PowerUp", cast);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }
}
