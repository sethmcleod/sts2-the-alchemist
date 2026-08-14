using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Compat;

/// <summary>
/// ONE OF THE TWO FILES THAT DIFFER BETWEEN THE beta AND main BRANCHES (the other is
/// WeakSpotPowerCompat.cs). THIS COPY IS THE beta IMPLEMENTATION.
/// </summary>
/// <remarks>
/// The game's default branch and its public-beta branch spell a handful of damage and animation
/// APIs differently. Every gameplay caller goes through this class instead, so the two branches
/// differ here and nowhere else, and a merge from beta into main conflicts in this file alone
/// rather than in the eighteen files that call these APIs.
///
/// beta: the damage APIs take a trailing CardPlay, and Spine track entries are IDisposable and
/// come from AddAnimationTracked. main: no CardPlay, and AddAnimation returns a plain entry.
///
/// When the branches converge, delete both files and inline the calls again.
///
/// Differences that needed no shim, because one spelling compiles on both branches, live in the
/// ordinary source: <c>cardPlay.Card.Owner</c> rather than <c>cardPlay.Player</c>.
/// </remarks>
public static class GameCompat
{
    /// <summary>main stops at cardSource; beta takes the play as well.</summary>
    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? play = null) =>
        CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource, play);

    /// <summary>main has no play parameter between cardSource and the hook type.</summary>
    public static decimal ModifyDamage(IRunState runState, ICombatState? combatState, Creature? target,
        Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? play,
        ModifyDamageHookType hookType, CardPreviewMode previewMode,
        out IEnumerable<AbstractModel> modifiers) =>
        Hook.ModifyDamage(runState, combatState, target, dealer, damage, props, cardSource, play, hookType,
            previewMode, out modifiers);

    /// <summary>main requires the blacklist argument; beta defaults it.</summary>
    public static IEnumerable<PotionModel> GetPotionOptions(Player player) =>
        PotionFactory.GetPotionOptions(player);

    // The attack builder's FromCard takes the play on beta and not on main. No shim is needed for
    // it: main declares an extension method with this two-argument shape, and on beta the real
    // instance method wins over any extension, so the call sites read the same on both branches.

    /// <summary>
    /// Queues one blink and returns how long it lasts. Wrapped because the track entry is
    /// IDisposable on beta and not on main, so the disposal cannot live at the call site.
    /// </summary>
    public static float QueueBlink(MegaAnimationState state, string blink, float delay, int track)
    {
        using var entry = state.AddAnimationTracked(blink, delay, loop: false, track);
        // The blink swaps the eye attachment, and an attachment must snap rather than fade
        entry.SetMixDuration(0f);
        return entry.GetAnimationDuration();
    }

    /// <summary>
    /// Starts the current animation at a random point, the way the base game staggers two
    /// characters at one rest site. Wrapped for the same disposal reason as QueueBlink.
    /// </summary>
    public static void RandomiseTrackStart(MegaAnimationState state, Random rng)
    {
        using var entry = state.GetCurrent(0);
        entry?.SetTrackTime(entry.GetAnimationEnd() * (float)rng.NextDouble());
    }
}
