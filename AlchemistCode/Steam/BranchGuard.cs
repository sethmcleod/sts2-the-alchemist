using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;

namespace Alchemist.AlchemistCode.Steam;

// An override whose base is missing on the running game branch silently becomes a new slot the game never calls
internal static class BranchGuard
{
    // Must match workshop/targets.json
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

    // C# marks its own virtuals NewSlot, so only an override that lost its base resolves to itself
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

    // Steam reports the default branch as an empty name or "public" depending on the client
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
        WebPage.Open(url);
    }
}
