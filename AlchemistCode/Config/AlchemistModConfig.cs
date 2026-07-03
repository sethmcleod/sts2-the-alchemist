using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Config;
using Godot;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Config;

/// <summary>
/// Mod settings page (registered in <see cref="MainFile"/>). Testing helpers that mark every
/// Alchemist card/relic/potion as "seen" (so they show in the compendium) or clear that state.
/// Content is enumerated by base type — no ID-prefix assumptions. Deliberately does NOT touch the
/// base-game Timeline/epochs, so it can't affect vanilla progression.
/// </summary>
public class AlchemistModConfig : SimpleModConfig
{
    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer); // also picks up [ConfigButton] methods
        SetupFocusNeighbors(optionContainer);
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

        save.SaveProgressFile();
        Notify($"Unlocked {cards.Count} cards, {relics.Count} relics, and {potions.Count} potions.");
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

        save.SaveProgressFile();
        Notify("Re-locked all Alchemist cards, relics, and potions.");
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
