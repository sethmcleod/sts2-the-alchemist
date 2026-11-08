using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Patches;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.Core.Localization;
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
        CardArtPatches.RememberArtSource();
        ConfigChanged += (_, _) => CardArtPatches.RefreshPortraitsIfArtSourceChanged();
        OnConfigReloaded += CardArtPatches.RefreshPortraitsIfArtSourceChanged;
    }

    private static readonly NodePath SelfPath = new(".");

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);
        AddRestoreDefaultsButton(optionContainer);
        RefreshShownRows(optionContainer);
        KonamiCode.Listen(optionContainer, () => UnlockSecrets(optionContainer));

        // Subscribed after [ConfigVisibleIf] so these run after it, and BaseLib drops both lists on close
        EventHandler refreshOnChange = (_, _) => RefreshShownRows(optionContainer);
        Action refreshOnReload = () => RefreshShownRows(optionContainer);
        ConfigChanged += refreshOnChange;
        OnConfigReloaded += refreshOnReload;
        _configChangedHandlers.Add(refreshOnChange);
        _configReloadedHandlers.Add(refreshOnReload);
    }

    [ConfigSection("Timeline")]
    [ConfigHoverTip]
    public static bool EnableEpochs { get; set; } = true;

    [ConfigSection("Compatibility")]
    [ConfigHoverTip]
    public static bool KeepPoolsSeparate { get; set; } = true;

    [ConfigSection("Compatibility")]
    [ConfigHoverTip]
    public static bool HeroCardsWithoutExpansion { get; set; } = false;

    [ConfigSection("Compatibility")]
    [ConfigHoverTip]
    public static bool ReshuffledCardsWithoutMod { get; set; } = false;

    [ConfigSection("Appearance")]
    [ConfigHoverTip]
    public static bool UseBetaArt { get; set; } = false;

    [ConfigHideInUI]
    public static string BetaArtExceptions { get; set; } = "";

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    public static bool ShowPoisonForecast { get; set; } = true;

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(ShowPoisonForecast))]
    [ConfigColorPicker(EditAlpha = false)]
    public static Color PoisonForecastColor { get; set; } = new("76FF40");

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    public static bool ShowAllyAntitoxinBars { get; set; } = true;

    [ConfigSection("Accessibility")]
    [ConfigHoverTip]
    [ConfigColorPicker(EditAlpha = false)]
    public static Color AntitoxinBarColor { get; set; } = new("9B5CFF");

    [ConfigSection("Analytics")]
    [ConfigHoverTip]
    public static bool AnalyticsEnabled { get; set; } = true;

    [ConfigHideInUI]
    [ConfigIgnoreRestoreDefaults]
    public static bool AnalyticsPromptAnswered { get; set; } = false;

    [ConfigSection("Secrets")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(SecretsUnlocked))]
    public static bool BigHeadMode { get; set; } = false;

    [ConfigSection("Secrets")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(BigHeadSizeShown))]
    [ConfigSlider(1.5, 2.5, 0.25, Format = "{0:0%}")]
    public static double BigHeadSize { get; set; } = 1.5;

    [ConfigSection("Secrets")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(SecretsUnlocked))]
    public static bool RainbowEyes { get; set; } = false;

    [ConfigSection("Secrets")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(SecretsUnlocked))]
    public static bool RainbowOrb { get; set; } = false;

    [ConfigSection("Secrets")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(SecretsUnlocked))]
    public static bool RainbowSkin { get; set; } = false;

    [ConfigSection("Secrets")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(SecretsUnlocked))]
    public static bool RainbowRobes { get; set; } = false;

    [ConfigSection("Secrets")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(SecretsUnlocked))]
    public static bool RainbowBag { get; set; } = false;

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

        RemoveFromDiscovered("_discoveredCards", ModelDb.AllCards.Where(c => c is AlchemistCard).Select(c => c.Id));
        RemoveFromDiscovered("_discoveredRelics", ModelDb.AllRelics.Where(r => r is AlchemistRelic).Select(r => r.Id));
        RemoveFromDiscovered("_discoveredPotions", ModelDb.AllPotions.Where(p => p is AlchemistPotion).Select(p => p.Id));

        // Removed outright: NotObtained would draw 2-7 as locked slots up front
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

    // Without this, a profile past Neow gets Alchemist1 as ObtainedNoSlot and Singleplayer stays locked
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

    // Not BaseLib's message popup, which is titled "Mod configuration error"
    private static void Notify(string message)
    {
        MainFile.Logger.Info("[Config] " + message);
        var popup = NErrorPopup.Create("Success", message, false);
        if (popup != null && NModalContainer.Instance != null)
            NModalContainer.Instance.Add((Node)(object)popup, true);
    }

    private static bool SecretsUnlocked() => SecretUnlocks.IsUnlocked;

    private static bool BigHeadSizeShown() => SecretUnlocks.IsUnlocked && BigHeadMode;

    private void UnlockSecrets(Control optionContainer)
    {
        if (SecretUnlocks.IsUnlocked || !GodotObject.IsInstanceValid(optionContainer)) return;
        if (!SecretUnlocks.Unlock()) return;

        ConfigReloaded();
        ConfigToast.Show(optionContainer, new LocString("settings_ui", "ALCHEMIST-SECRETS_UNLOCKED_TOAST"));
    }

    private static void RefreshShownRows(Control optionContainer)
    {
        ShowDividersBetweenShownRows(optionContainer);
        LinkShownFocusNeighbors(optionContainer);
    }

    // BaseLib hides a divider unless the rows on both sides show, so a hidden row takes both its dividers
    private static void ShowDividersBetweenShownRows(Control optionContainer)
    {
        foreach (var section in optionContainer.GetChildren().OfType<NConfigCollapsibleSection>())
        {
            var shownRowAbove = false;
            ColorRect? divider = null;
            foreach (var child in section.ContentContainer.GetChildren())
            {
                if (child is ColorRect line)
                {
                    line.Visible = false;
                    divider = line;
                }
                else if (child is NConfigOptionRow { Visible: true })
                {
                    if (shownRowAbove && divider != null) divider.Visible = true;
                    shownRowAbove = true;
                    divider = null;
                }
            }
        }
    }

    // BaseLib's SetupFocusNeighbors also links hidden rows, so focus could land on one
    private static void LinkShownFocusNeighbors(Control optionContainer)
    {
        var controls = ShownFocusables(optionContainer).ToList();
        for (var i = 0; i < controls.Count; i++)
        {
            controls[i].FocusNeighborTop = controls[(i + controls.Count - 1) % controls.Count].GetPath();
            controls[i].FocusNeighborBottom = controls[(i + 1) % controls.Count].GetPath();
            controls[i].FocusNeighborLeft = SelfPath;
            controls[i].FocusNeighborRight = SelfPath;
        }
    }

    private static IEnumerable<Control> ShownFocusables(Node node)
    {
        if (node is Control { Visible: false }) yield break;

        if (node is Control { FocusMode: Control.FocusModeEnum.All } control)
        {
            yield return control;
            yield break;
        }

        foreach (var child in node.GetChildren())
            foreach (var focusable in ShownFocusables(child))
                yield return focusable;
    }
}
