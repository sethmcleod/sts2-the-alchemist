using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;

namespace Alchemist.AlchemistCode.Steam;

// The game's two Steam branches spell a few virtual hooks differently, and the Compat/ files carry one
// spelling per git branch. A build that lands on the other game branch does not fail: an `override`
// whose base method is missing at runtime silently becomes a new virtual slot, the game never calls it,
// and the effect behind it (Antitoxin's absorb, Fallout's bonus, Steep's redirect) just stops. The
// player sees a mod that "does not work" with nothing in the log. So at boot this walks the assembly for
// overrides that found no base, logs them, and offers the matching Workshop item through the game's own
// popup. The Steam branch-range fields in the Workshop metadata were tried for this and rejected: they
// strand players on old versions until they reinstall the game.
//
// Debug flag: --alchemist-test-branch-popup shows the popup at once, mismatch or not
internal static class BranchGuard
{
    // Mirrors workshop/targets.json, the one place that knows both items
    private const ulong BetaItemId = 3780726901;
    private const ulong MainItemId = 3781688551;
    private const string BetaBranch = "public-beta";
    private const string PublicBranch = "public";
    private static readonly TimeSpan PopupRetry = TimeSpan.FromSeconds(1);

    public static void Initialize()
    {
        if (CommandLineHelper.HasArg("alchemist-test-branch-popup"))
        {
            MainFile.Logger.Info("[BranchGuard] test flag: showing the branch popup.");
            TaskHelper.RunSafely(ShowPopup());
            return;
        }
        var orphans = FindOrphanedOverrides();
        if (orphans.Count == 0)
        {
            MainFile.Logger.Info("[BranchGuard] Every override matches the running game build.");
            return;
        }
        MainFile.Logger.Error("[BranchGuard] This build was compiled against the other Steam branch of the "
            + "game: these overrides have no base method in the running build and will never be called: "
            + string.Join(", ", orphans));
        TaskHelper.RunSafely(ShowPopup());
    }

    // An `override` compiles to a virtual method without the NewSlot flag. When the base it was written
    // against exists, GetBaseDefinition walks up to it; when the running game lacks it, the walk ends at
    // the method itself. C# marks its own `virtual` declarations NewSlot, so those never match
    internal static List<string> FindOrphanedOverrides()
    {
        Type?[] types;
        try
        {
            types = typeof(MainFile).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types;
        }
        var orphans = new List<string>();
        foreach (var type in types)
        {
            if (type == null || type.IsInterface) continue;
            const BindingFlags declared = BindingFlags.DeclaredOnly | BindingFlags.Instance
                | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var method in type.GetMethods(declared))
            {
                if (!method.IsVirtual || method.IsAbstract) continue;
                if ((method.Attributes & MethodAttributes.NewSlot) != 0) continue;
                if (method.GetBaseDefinition() != method) continue;
                orphans.Add($"{type.Name}.{method.Name}");
            }
        }
        return orphans;
    }

    // "public" is the default branch; Steam reports it as an empty name or as "public" depending on
    // the client, so anything that is not the beta key is read as public
    private static string CurrentBranch()
    {
        if (!SteamInitializer.Initialized) return PublicBranch;
        if (!SteamApps.GetCurrentBetaName(out var name, 128)) return PublicBranch;
        return string.IsNullOrWhiteSpace(name) ? PublicBranch : name;
    }

    private static async Task ShowPopup()
    {
        while (NGame.Instance?.MainMenu == null || NModalContainer.Instance == null
               || LocManager.Instance == null)
            await Task.Delay(PopupRetry);

        var branch = CurrentBranch();
        MainFile.Logger.Info($"[BranchGuard] Showing the branch popup over the main menu (branch '{branch}').");
        var popup = NGenericPopup.Create();
        if (popup == null) return;
        NModalContainer.Instance.Add(popup);
        var body = new LocString("settings_ui", "ALCHEMIST-BRANCH_MISMATCH.body");
        body.Add("currentBranch", branch);
        var open = await popup.WaitForConfirmation(
            body,
            new LocString("settings_ui", "ALCHEMIST-BRANCH_MISMATCH.header"),
            new LocString("settings_ui", "ALCHEMIST-BRANCH_MISMATCH.continue"),
            new LocString("settings_ui", "ALCHEMIST-BRANCH_MISMATCH.open"));
        if (!open) return;

        var itemId = branch == BetaBranch ? BetaItemId : MainItemId;
        var url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={itemId}";
        MainFile.Logger.Info($"[BranchGuard] Opening the Workshop item for branch '{branch}': {url}");
        if (SteamInitializer.Initialized && SteamUtils.IsOverlayEnabled())
            SteamFriends.ActivateGameOverlayToWebPage(url);
        else
            OS.ShellOpen(url);
    }
}
