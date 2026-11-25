using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;

namespace Alchemist.AlchemistCode.Compat;

static class FtueCompat
{
#if STS2_V0114
    internal static NFtue NewRoot() => new AlchemistFtueRoot { FocusMode = Control.FocusModeEnum.All };

    internal static void AfterAdd(NFtue root)
    {
        var path = root.GetPath();
        root.FocusNeighborBottom = path;
        root.FocusNeighborTop = path;
        root.FocusNeighborLeft = path;
        root.FocusNeighborRight = path;
        if (NHotkeyManager.Instance is not { } hotkeys) return;
        hotkeys.AddBlockingScreen(root);
        root.TreeExiting += () => NHotkeyManager.Instance?.RemoveBlockingScreen(root);
    }
#else
    internal static NFtue NewRoot() => new NFtue();

    internal static void AfterAdd(NFtue root)
    {
    }
#endif
}
