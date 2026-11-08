using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Compat;

// Wraps the game APIs that the beta and main game branches spell differently
// Each Compat file is branch-specific: on a merge, keep the target branch's copy
public static class GameCompat
{
    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? play = null) =>
        CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource, play);

    public static Task LoseBlock(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        Creature? remover) =>
        CreatureCmd.LoseBlock(choiceContext, target, amount, remover);

    public static Task TriggerPoison(PoisonPower poison) => poison.Trigger();

    public static decimal ModifyDamage(IRunState runState, ICombatState? combatState, Creature? target,
        Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? play,
        ModifyDamageHookType hookType, CardPreviewMode previewMode,
        out IEnumerable<AbstractModel> modifiers) =>
        Hook.ModifyDamage(runState, combatState, target, dealer, damage, props, cardSource, play, hookType,
            previewMode, out modifiers);

    public static IEnumerable<PotionModel> GetPotionOptions(Player player) =>
        PotionFactory.GetPotionOptions(player);

    public static float QueueBlink(MegaAnimationState state, string blink, float delay, int track)
    {
        using var entry = state.AddAnimationTracked(blink, delay, loop: false, track);
        // An attachment swap must snap, not fade
        entry.SetMixDuration(0f);
        return entry.GetAnimationDuration();
    }

    public static void RandomiseTrackStart(MegaAnimationState state, Random rng)
    {
        using var entry = state.GetCurrent(0);
        entry?.SetTrackTime(entry.GetAnimationEnd() * (float)rng.NextDouble());
    }

    public static bool IsDirectionalNavigation(NControllerManager controllers) =>
        controllers.IsUsingDirectionalNavigation;

    public static void ToggleTick(NTickbox tickbox) => tickbox.ForceToggleTick();

    public static void UpdateHotkeyIcon(Control icon, string input) => (icon as NHotkeyIcon)?.UpdateInput(input);
}
