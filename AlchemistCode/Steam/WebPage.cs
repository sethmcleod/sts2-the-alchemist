using Godot;
using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;

namespace Alchemist.AlchemistCode.Steam;

internal static class WebPage
{
    public static void Open(string url)
    {
        if (SteamInitializer.Initialized && SteamUtils.IsOverlayEnabled())
            SteamFriends.ActivateGameOverlayToWebPage(url);
        else
            OS.ShellOpen(url);
    }
}
