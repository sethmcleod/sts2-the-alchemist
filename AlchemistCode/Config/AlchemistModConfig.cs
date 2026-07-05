using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Config;
using Godot;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Config;

/// <summary>
/// Mod settings page (registered in <see cref="MainFile"/>). Testing helpers that mark every
/// Alchemist card/relic/potion as "seen" (so they show in the compendium) or clear that state.
/// Content is enumerated by base type — no ID-prefix assumptions. Also reveals/hides the Alchemist's own
/// Timeline epochs (so epoch-gated content is testable); it never touches base-game epochs.
/// </summary>
public class AlchemistModConfig : SimpleModConfig
{
    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer); // also picks up [ConfigButton] methods
        SetupFocusNeighbors(optionContainer);
    }

    /// <summary>Master switch for the Timeline/Epoch feature (OFF by default). While off, the Alchemist's
    /// epochs never appear on the Timeline and never gate content — so they can't clutter the Timeline or
    /// collide with other mods. When first turned on, all epochs start Revealed (nothing gets locked); a
    /// "Reset Unlocks" then drops into the milestone-progression mode. Read at runtime by
    /// <see cref="Patches.EpochPatches"/>. BaseLib config properties are static + auto-persisted.</summary>
    [ConfigSection("Timeline")]
    [ConfigHoverTip] // shows the "...hover.desc" loc as a popup on hover (like BaseLib's own settings)
    public static bool EnableEpochs { get; set; } = true;

    [ConfigSection("Unlocks")]
    [ConfigButton("UnlockAllButtonLabel")]
    public static void UnlockAll()
    {
        var save = SaveManager.Instance;
        if (save == null) return;

        var cards = ModelDb.AllCards.Where(c => c is AlchemistCard).ToList();
        var relics = ModelDb.AllRelics.Where(r => r is AlchemistRelic).ToList();
        var potions = ModelDb.AllPotions.Where(p => p is AlchemistPotion).ToList();

        foreach (var card in cards) save.MarkCardAsSeen(card);
        foreach (var relic in relics) save.MarkRelicAsSeen(relic);
        foreach (var potion in potions) save.MarkPotionAsSeen(potion);

        // Also reveal this character's Timeline epochs so epoch-gated content is usable without grinding
        // the milestones (IsEpochRevealed(Revealed) == true unlocks the gated cards/relics/potions).
        foreach (var type in EpochRegistration.AlchemistEpochTypes)
            save.ObtainEpochOverride(EpochModel.GetId(type), EpochState.Revealed);

        save.SaveProgressFile();
        Notify($"Unlocked {cards.Count} cards, {relics.Count} relics, {potions.Count} potions, and all Epochs.");
    }

    [ConfigSection("Unlocks")]
    [ConfigButton("ResetUnlocksButtonLabel", Color = "#b03f3f")]
    public static void ResetUnlocks()
    {
        var save = SaveManager.Instance;
        if (save?.Progress == null) return;

        // DiscoveredCards/Relics/Potions are read-only; strip just this mod's ids from the private sets.
        RemoveFromDiscovered("_discoveredCards", ModelDb.AllCards.Where(c => c is AlchemistCard).Select(c => c.Id));
        RemoveFromDiscovered("_discoveredRelics", ModelDb.AllRelics.Where(r => r is AlchemistRelic).Select(r => r.Id));
        RemoveFromDiscovered("_discoveredPotions", ModelDb.AllPotions.Where(p => p is AlchemistPotion).Select(p => p.Id));

        // Re-lock the Timeline epochs too (re-gates the epoch-locked content).
        foreach (var type in EpochRegistration.AlchemistEpochTypes)
            save.ObtainEpochOverride(EpochModel.GetId(type), EpochState.NotObtained);

        save.SaveProgressFile();
        Notify("Re-locked all Alchemist cards, relics, potions, and Epochs.");
    }

    private static void RemoveFromDiscovered(string fieldName, IEnumerable<ModelId> ids)
    {
        var progress = SaveManager.Instance.Progress;
        var field = typeof(ProgressState).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(progress) is not HashSet<ModelId> set) return;
        var toRemove = ids.ToHashSet();
        set.RemoveWhere(toRemove.Contains);
    }

    /// <summary>Raise a "Success" popup. (BaseLib's auto PendingUserMessages popup is hard-titled
    /// "Mod configuration error", so we create our own instead.)</summary>
    private static void Notify(string message)
    {
        MainFile.Logger.Info("[Config] " + message);
        var popup = NErrorPopup.Create("Success", message, false);
        if (popup != null && NModalContainer.Instance != null)
            NModalContainer.Instance.Add((Node)(object)popup, true);
    }
}
