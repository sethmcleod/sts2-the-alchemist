using BaseLib.Config.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Alchemist.AlchemistCode.Config;

// A copy of the settings toast: the submenu stack hides the settings screen while the mod config is open
internal static class ConfigToast
{
    // Must outlast the 2.25 s tween in NSettingsToast.Show
    private const double Lifetime = 3.0;

    private static readonly AccessTools.FieldRef<NSettingsToast, float> OriginalYRef =
        AccessTools.FieldRefAccess<NSettingsToast, float>("_originalY");

    public static void Show(Node page, LocString text)
    {
        var submenu = FindAncestor<NModConfigSubmenu>(page);
        if (submenu?.GetParent() is not NSubmenuStack stack) return;

        var template = stack.GetSubmenuType<NSettingsScreen>().GetNodeOrNull<NSettingsToast>("%Toast");
        if (template == null)
        {
            MainFile.Logger.Info("The settings screen has no toast node, thus no toast shows.");
            return;
        }

        var toast = (NSettingsToast)template.Duplicate();

        // Show raises the toast from where _Ready found it, and the original never drops back down
        var drift = OriginalYRef(template) - template.Position.Y;
        toast.OffsetTop += drift;
        toast.OffsetBottom += drift;

        toast.MouseFilter = Control.MouseFilterEnum.Ignore;
        if (toast.GetNodeOrNull<Control>("Label") is { } label) label.MouseFilter = Control.MouseFilterEnum.Ignore;

        submenu.AddChild(toast);
        toast.Show(text);

        submenu.GetTree().CreateTimer(Lifetime).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(toast)) toast.QueueFree();
        };
    }

    private static T? FindAncestor<T>(Node node) where T : Node
    {
        for (var parent = node.GetParent(); parent != null; parent = parent.GetParent())
            if (parent is T match) return match;

        return null;
    }
}
