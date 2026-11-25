using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Alchemist.AlchemistCode.Compat;

static class HotkeyCompat
{
#if STS2_V0114
    internal static void PushPressed(string hotkey, Node owner, Action action) =>
        NHotkeyManager.Instance?.PushHotkeyPressedBinding(hotkey, owner, action);

    internal static void RemovePressed(string hotkey, Node owner, Action action) =>
        NHotkeyManager.Instance?.RemoveHotkeyPressedBinding(hotkey, owner, action);
#else
    internal static void PushPressed(string hotkey, Node owner, Action action) =>
        NHotkeyManager.Instance?.PushHotkeyPressedBinding(hotkey, action);

    internal static void RemovePressed(string hotkey, Node owner, Action action) =>
        NHotkeyManager.Instance?.RemoveHotkeyPressedBinding(hotkey, action);
#endif
}
