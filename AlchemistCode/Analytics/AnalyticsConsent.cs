using System.Threading.Tasks;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Steam;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Analytics;

internal static class AnalyticsConsent
{
    private const string SiteUrl = "https://alchemist.fyi";
    private const int SaveDelayMs = 1000;

    private static bool _asking;

    public static void AskOnce()
    {
        if (_asking || AlchemistModConfig.AnalyticsPromptAnswered || !AnalyticsEndpoint.IsConfigured
            || !SaveManager.Instance.PrefsSave.UploadData) return;
        _asking = true;
        TaskHelper.RunSafely(Ask());
    }

    private static async Task Ask()
    {
        try
        {
            var answer = await ShowPopup("ALCHEMIST-ANALYTICS_PROMPT",
                new LocString("main_menu_ui", "GENERIC_POPUP.cancel"),
                new LocString("main_menu_ui", "GENERIC_POPUP.confirm"));
            if (answer is not { } share) return;

            AlchemistModConfig.AnalyticsEnabled = share;
            AlchemistModConfig.AnalyticsPromptAnswered = true;
            ModConfig.SaveDebounced<AlchemistModConfig>(SaveDelayMs);
            MainFile.Logger.Info($"Alchemist analytics prompt answered: sharing {(share ? "on" : "off")}.");

            await ShowPopup(share ? "ALCHEMIST-ANALYTICS_ON" : "ALCHEMIST-ANALYTICS_OFF",
                null,
                new LocString("main_menu_ui", "GENERIC_POPUP.ok"));
        }
        finally
        {
            _asking = false;
        }
    }

    private static async Task<bool?> ShowPopup(string locKey, LocString? noButton, LocString yesButton)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        if (NModalContainer.Instance is not { OpenModal: null } modals || NGenericPopup.Create() is not { } popup)
            return null;

        modals.Add(popup);
        var answer = popup.WaitForConfirmation(
            new LocString("settings_ui", locKey + ".body"),
            new LocString("settings_ui", locKey + ".header"),
            noButton,
            yesButton);

        var body = popup.GetNode<RichTextLabel>("VerticalPopup/Description");
        body.MouseFilter = Control.MouseFilterEnum.Stop;
        body.MetaClicked += _ => WebPage.Open(SiteUrl);

        return await answer;
    }
}
