using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Compat;
using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(NInspectCardScreen))]
public static class BetaArtTickboxPatches
{
    private const float Gap = 80f;

    private sealed class Row
    {
        public required NTickbox Tickbox;
        public required Control? HotkeyIcon;
        public required float UpgradeLeft;
        public required float UpgradeRight;
        public float? PlacedUpgradeLeft;
        public bool Ready;
        public Callable DeviceChanged;

        public void ToggleFromHotkey()
        {
            if (GodotObject.IsInstanceValid(Tickbox) && Tickbox.IsVisibleInTree()) GameCompat.ToggleTick(Tickbox);
        }
    }

    private static readonly ConditionalWeakTable<NInspectCardScreen, Row> Rows = new();

    private static readonly AccessTools.FieldRef<NInspectCardScreen, NTickbox> UpgradeTickbox =
        AccessTools.FieldRefAccess<NInspectCardScreen, NTickbox>("_upgradeTickbox");

    private static readonly AccessTools.FieldRef<NInspectCardScreen, List<CardModel>?> Cards =
        AccessTools.FieldRefAccess<NInspectCardScreen, List<CardModel>?>("_cards");

    private static readonly AccessTools.FieldRef<NInspectCardScreen, int> Index =
        AccessTools.FieldRefAccess<NInspectCardScreen, int>("_index");

    private static readonly Action<NButton>? UnregisterHotkeys = FindUnregisterHotkeys();

    private static Action<NButton>? FindUnregisterHotkeys()
    {
        if (AccessTools.Method(typeof(NButton), "UnregisterHotkeys") is { } method)
            return AccessTools.MethodDelegate<Action<NButton>>(method);
        MainFile.Logger.Error(
            "NButton.UnregisterHotkeys is missing, so the Beta Art tickbox is off. A copy of the View Upgrades tickbox would take its confirm hotkey");
        return null;
    }

    [HarmonyPatch("Open")] [HarmonyPostfix] [HarmonyPriority(Priority.Last)]
    private static void AfterOpen(NInspectCardScreen __instance)
    {
        SafeSync(__instance);
        if (Rows.TryGetValue(__instance, out var row))
            NHotkeyManager.Instance?.PushHotkeyPressedBinding(MegaInput.select, row.ToggleFromHotkey);
    }

    [HarmonyPatch("SetCard")] [HarmonyPostfix] [HarmonyPriority(Priority.Last)]
    private static void AfterSetCard(NInspectCardScreen __instance) => SafeSync(__instance);

    [HarmonyPatch("Close")] [HarmonyPostfix]
    private static void AfterClose(NInspectCardScreen __instance)
    {
        if (Rows.TryGetValue(__instance, out var row))
            NHotkeyManager.Instance?.RemoveHotkeyPressedBinding(MegaInput.select, row.ToggleFromHotkey);
    }

    private static void SafeSync(NInspectCardScreen screen)
    {
        try
        {
            Sync(screen);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Beta Art tickbox failed: {e}");
        }
    }

    private static void Sync(NInspectCardScreen screen)
    {
        var upgrade = UpgradeTickbox(screen);
        if (!GodotObject.IsInstanceValid(upgrade)) return;
        if (!Rows.TryGetValue(screen, out var row) || !GodotObject.IsInstanceValid(row.Tickbox))
        {
            Rows.Remove(screen);
            if (CreateRow(screen, upgrade) is not { } created) return;
            row = created;
            Rows.Add(screen, row);
            var ready = row;
            Callable.From(() => Callable.From(() =>
            {
                ready.Ready = true;
                SafeSync(screen);
            }).CallDeferred()).CallDeferred();
        }

        if (!row.Ready) return;

        var card = CurrentCard(screen) as AlchemistCard;
        var shown = card is { HasFinalAndBetaArt: true };
        row.Tickbox.Visible = shown;
        row.Tickbox.MouseFilter = shown ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        if (card != null && shown) row.Tickbox.IsTicked = card.ShowsBetaArt;
        Place(row, upgrade, shown);
    }

    private static CardModel? CurrentCard(NInspectCardScreen screen)
    {
        var cards = Cards(screen);
        var index = Index(screen);
        return cards != null && index >= 0 && index < cards.Count ? cards[index] : null;
    }

    private static Row? CreateRow(NInspectCardScreen screen, NTickbox upgrade)
    {
        if (UnregisterHotkeys is not { } unregisterHotkeys) return null;
        const Node.DuplicateFlags flags =
            Node.DuplicateFlags.Groups | Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation;
        if (upgrade.Duplicate((int)flags) is not NTickbox tickbox) return null;

        tickbox.Name = "AlchemistBetaArt";
        tickbox.FocusMode = Control.FocusModeEnum.None;
        tickbox.Visible = false;
        tickbox.Modulate = Colors.White;
        Adopt(upgrade, tickbox, tickbox);
        var hotkeyIcon = (tickbox.FindChild("HotkeyIcon", true, false) ?? tickbox.FindChild("ControllerIcon", true, false)) as Control;
        if (hotkeyIcon != null)
        {
            hotkeyIcon.UniqueNameInOwner = false;
            hotkeyIcon.Name = "BetaArtHotkeyIcon";
        }
        if (tickbox.FindChild("TickboxVisuals", true, false) is CanvasItem { Material: ShaderMaterial hsv } visuals)
            visuals.Material = (Material)hsv.Duplicate();

        var row = new Row
        {
            Tickbox = tickbox,
            HotkeyIcon = hotkeyIcon,
            UpgradeLeft = upgrade.OffsetLeft,
            UpgradeRight = upgrade.OffsetRight,
        };
        row.DeviceChanged = Callable.From(() => OnDeviceChanged(screen, row));
        tickbox.TreeEntered += () => ConnectDeviceSignals(row.DeviceChanged);
        tickbox.TreeExiting += () => DisconnectDeviceSignals(row.DeviceChanged);

        upgrade.AddSibling(tickbox);
        unregisterHotkeys(tickbox);
        if (tickbox.FindChild("ShowUpgradeLabel", true, false) is MegaLabel label)
            label.SetTextAutoSize(new LocString("gameplay_ui", "BETA_ART_TICKBOX.label").GetFormattedText());
        tickbox.IsTicked = false;
        tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(box => OnToggled(screen, box.IsTicked)));
        RefreshHotkeyIcon(row);
        return row;
    }

    private static void ConnectDeviceSignals(Callable callable)
    {
        Connect(NControllerManager.Instance, NControllerManager.SignalName.MouseDetected, callable);
        Connect(NControllerManager.Instance, NControllerManager.SignalName.ControllerDetected, callable);
        Connect(NInputManager.Instance, NInputManager.SignalName.InputRebound, callable);
    }

    private static void DisconnectDeviceSignals(Callable callable)
    {
        Disconnect(NControllerManager.Instance, NControllerManager.SignalName.MouseDetected, callable);
        Disconnect(NControllerManager.Instance, NControllerManager.SignalName.ControllerDetected, callable);
        Disconnect(NInputManager.Instance, NInputManager.SignalName.InputRebound, callable);
    }

    private static void Connect(GodotObject? source, StringName signal, Callable callable)
    {
        if (GodotObject.IsInstanceValid(source) && !source!.IsConnected(signal, callable)) source.Connect(signal, callable);
    }

    private static void Disconnect(GodotObject? source, StringName signal, Callable callable)
    {
        if (GodotObject.IsInstanceValid(source) && source!.IsConnected(signal, callable)) source.Disconnect(signal, callable);
    }

    private static void OnDeviceChanged(NInspectCardScreen screen, Row row)
    {
        RefreshHotkeyIcon(row);
        SafeSync(screen);
    }

    private static void RefreshHotkeyIcon(Row row)
    {
        if (row.HotkeyIcon is not { } icon || !GodotObject.IsInstanceValid(icon)) return;
        icon.Visible = NControllerManager.Instance is { } controllers && GameCompat.IsDirectionalNavigation(controllers);
        GameCompat.UpdateHotkeyIcon(icon, MegaInput.select);
    }

    private static void Adopt(Node source, Node copy, Node owner)
    {
        var count = Math.Min(source.GetChildCount(), copy.GetChildCount());
        for (var i = 0; i < count; i++)
        {
            var sourceChild = source.GetChild(i);
            var copyChild = copy.GetChild(i);
            copyChild.Owner ??= owner;
            if (sourceChild.UniqueNameInOwner) copyChild.UniqueNameInOwner = true;
            Adopt(sourceChild, copyChild, owner);
        }
    }

    private static void Place(Row row, Control upgrade, bool shown)
    {
        if (!shown)
        {
            if (row.PlacedUpgradeLeft is { } placed && Mathf.IsEqualApprox(upgrade.OffsetLeft, placed))
                SetSpan(upgrade, row.UpgradeLeft, row.UpgradeRight);
            row.PlacedUpgradeLeft = null;
            return;
        }

        var boxWidth = row.Tickbox.GetCombinedMinimumSize().X;
        if (!upgrade.Visible)
        {
            SetSpan(row.Tickbox, -boxWidth / 2, boxWidth / 2);
            return;
        }

        var upgradeWidth = upgrade.GetCombinedMinimumSize().X;
        var left = -(upgradeWidth + Gap + boxWidth) / 2;
        SetSpan(upgrade, left, left + upgradeWidth);
        SetSpan(row.Tickbox, left + upgradeWidth + Gap, left + upgradeWidth + Gap + boxWidth);
        row.PlacedUpgradeLeft = upgrade.OffsetLeft;
    }

    private static void SetSpan(Control control, float left, float right)
    {
        control.OffsetLeft = left;
        control.OffsetRight = right;
    }

    private static void OnToggled(NInspectCardScreen screen, bool ticked)
    {
        try
        {
            if (CurrentCard(screen) is not AlchemistCard { HasFinalAndBetaArt: true } card) return;
            if (BetaArtChoices.Choose(card.Id.Entry, ticked)) CardArtPatches.RepaintCard(card.Id.Entry);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Beta Art toggle failed: {e}");
        }
    }
}
