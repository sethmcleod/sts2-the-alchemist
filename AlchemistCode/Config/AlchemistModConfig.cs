using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Patches;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace Alchemist.AlchemistCode.Config;

public class AlchemistModConfig : SimpleModConfig
{
    public AlchemistModConfig()
    {
        // Every tickbox raises ConfigChanged, so the cards already on screen repaint at once
        ConfigChanged += (_, _) => CardArtPatches.RefreshPortraitsIfArtSourceChanged();
    }

    public override void SetupConfigUI(Control optionContainer)
    {
        // Auto-generates the UI from the properties and [ConfigButton] methods below
        GenerateOptionsForAllProperties(optionContainer);
        AddRestoreDefaultsButton(optionContainer);
        SetupFocusNeighbors(optionContainer);
    }

    [ConfigSection("Timeline")]
    [ConfigHoverTip]
    public static bool EnableEpochs { get; set; } = true;

    [ConfigSection("Compatibility")]
    [ConfigHoverTip]
    public static bool KeepPoolsSeparate { get; set; } = true;

    // The nine cards made for The Hero Expansion normally leave the pool when that mod is absent.
    // This offers them anyway; the Broken Blade and Flashlight cards still need the mod to appear
    [ConfigSection("Compatibility")]
    [ConfigHoverTip]
    public static bool HeroCardsWithoutExpansion { get; set; } = false;

    [ConfigSection("Appearance")]
    [ConfigHoverTip]
    public static bool UseBetaArt { get; set; } = false;

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    public static bool ShowPoisonForecast { get; set; } = true;

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    public static bool ShowAllyAntitoxinBars { get; set; } = true;

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    [ConfigColorPicker(EditAlpha = false)]
    public static Color AntitoxinBarColor { get; set; } = new("9B5CFF");

    // A second gate on top of the game's own "Upload Data" setting, see Analytics/AlchemistMetrics.cs
    [ConfigSection("Analytics")]
    [ConfigHoverTip]
    public static bool AnalyticsEnabled { get; set; } = true;

    // Shown above Unlock All: opens the Timeline without granting the card, relic, and potion unlocks
    [ConfigSection("Unlocks")]
    [ConfigButton("RevealTimelineButtonLabel")]
    public static void RevealTimeline()
    {
        var save = SaveManager.Instance;
        if (save == null) return;

        if (!EpochRegistration.Supported)
        {
            Notify("This build of the game has no Timeline for mods, so there is nothing to reveal.");
            return;
        }

        foreach (var type in EpochRegistration.AlchemistEpochTypes)
            save.ObtainEpochOverride(EpochModel.GetId(type), EpochState.Revealed);

        save.SaveProgressFile();
        Notify("Unlocked all 7 Alchemist Epochs on the Timeline.");
    }

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

        if (EpochRegistration.Supported)
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

        // The discovered sets are read-only, so strip this mod's ids from the private backing fields
        RemoveFromDiscovered("_discoveredCards", ModelDb.AllCards.Where(c => c is AlchemistCard).Select(c => c.Id));
        RemoveFromDiscovered("_discoveredRelics", ModelDb.AllRelics.Where(r => r is AlchemistRelic).Select(r => r.Id));
        RemoveFromDiscovered("_discoveredPotions", ModelDb.AllPotions.Where(p => p is AlchemistPotion).Select(p => p.Id));

        // Remove the entries outright rather than set NotObtained, which would render 2-7 as locked slots
        // up front. Progression then restarts clean, with only Alchemist1's slot back via Neow
        RemoveAlchemistEpochs();
        RestoreFirstChapterSlot();

        save.SaveProgressFile();
        Notify("Re-locked all Alchemist cards, relics, potions, and Epochs.");
    }

    private static void RemoveAlchemistEpochs()
    {
        var progress = SaveManager.Instance.Progress;
        var field = typeof(ProgressState).GetField("_epochs", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(progress) is not List<SerializableEpoch> epochs) return;
        if (!EpochRegistration.Supported) return;
        var ids = EpochRegistration.AlchemistEpochTypes.Select(EpochModel.GetId).ToHashSet();
        epochs.RemoveAll(e => ids.Contains(e.Id));
    }

    // Neow's reveal is what creates Alchemist1's slot, and it runs once per profile. On a profile where Neow
    // is already Revealed nothing in the session creates the slot again, so the next run awards Alchemist1
    // as ObtainedNoSlot: the Timeline draws no tile for it, yet the main menu counts it and disables
    // Singleplayer until it is revealed, which locks the player out. Re-create the slot as NotObtained,
    // which is what the save loader's FixMissingSlots does for a Revealed parent
    private static void RestoreFirstChapterSlot()
    {
        if (!EpochRegistration.Supported) return;
        var progress = SaveManager.Instance.Progress;
        if (!progress.IsEpochRevealed(EpochModel.GetId<NeowEpoch>())) return;
        progress.UnlockSlot(EpochModel.GetId<Alchemist1Epoch>());
    }

    private static void RemoveFromDiscovered(string fieldName, IEnumerable<ModelId> ids)
    {
        var progress = SaveManager.Instance.Progress;
        var field = typeof(ProgressState).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(progress) is not HashSet<ModelId> set) return;
        var toRemove = ids.ToHashSet();
        set.RemoveWhere(toRemove.Contains);
    }

    // Custom popup, because BaseLib's auto message popup is hard-titled "Mod configuration error"
    private static void Notify(string message)
    {
        MainFile.Logger.Info("[Config] " + message);
        var popup = NErrorPopup.Create("Success", message, false);
        if (popup != null && NModalContainer.Instance != null)
            NModalContainer.Instance.Add((Node)(object)popup, true);
    }
}
