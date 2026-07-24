using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Config;

public class AlchemistModConfig : SimpleModConfig
{
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

    [ConfigSection("Economy")]
    [ConfigHoverTip]
    public static bool UniversalPotionSelling { get; set; }

    [ConfigSection("Economy")]
    [ConfigHoverTip]
    [ConfigSlider(25.0, 100.0, 25.0, Format = "{0}%")]
    public static int PotionSellPercent { get; set; } = 100;

    [ConfigSection("Economy")]
    [ConfigHoverTip]
    [ConfigSlider(10.0, 50.0, 10.0, Format = "{0}%")]
    public static int BrewPotionChance { get; set; } = 30;

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    public static bool ShowDamageForecasts { get; set; } = true;

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    [ConfigColorPicker(EditAlpha = false)]
    public static Color ForecastColor { get; set; } = new("B15CD1");

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    public static bool ShowHandGlows { get; set; } = true;

    // Shown above Unlock All: opens the Timeline without granting the card, relic, and potion unlocks
    [ConfigSection("Unlocks")]
    [ConfigButton("RevealTimelineButtonLabel")]
    public static void RevealTimeline()
    {
        var save = SaveManager.Instance;
        if (save == null) return;

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

        // Remove the epoch entries entirely (rather than NotObtained, which would render 2-7 as locked
        // slots up front) so progression restarts clean: only Alchemist1's slot reappears, via Neow
        RemoveAlchemistEpochs();

        save.SaveProgressFile();
        Notify("Re-locked all Alchemist cards, relics, potions, and Epochs.");
    }

    private static void RemoveAlchemistEpochs()
    {
        var progress = SaveManager.Instance.Progress;
        var field = typeof(ProgressState).GetField("_epochs", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(progress) is not List<SerializableEpoch> epochs) return;
        var ids = EpochRegistration.AlchemistEpochTypes.Select(EpochModel.GetId).ToHashSet();
        epochs.RemoveAll(e => ids.Contains(e.Id));
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
