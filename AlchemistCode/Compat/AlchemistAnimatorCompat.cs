using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Character;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs and the two power compat files. An override cannot be
// routed through a wrapper, and here the whole body differs, so the method lives here rather than in
// Character/Alchemist.cs.
//
// THIS COPY IS THE main (DEFAULT BRANCH) IMPLEMENTATION. ON A MERGE FROM beta, KEEP THIS SIDE.
// The public branch cannot express the near death idle at all: GenerateAnimator is handed no
// Creature, CharacterModel has no IsLowHealth to ask, and AnimState has no AddNextState for a
// conditional fall-back. So every one shot returns to the one idle, and near_death_loop goes unplayed
public partial class Alchemist
{
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AlchemistVisuals.StartIdleFlourishes(controller);

        var idle = new AnimState(AlchemistVisuals.IdleAnimation, isLooping: true);

        AnimState Once(string? name) =>
            name == null ? idle : new AnimState(name) { NextState = idle };

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

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
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
