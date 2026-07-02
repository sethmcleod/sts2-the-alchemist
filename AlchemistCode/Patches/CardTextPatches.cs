using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// The base game renders a card's before-description keywords (Retain, Innate, …) as their own
/// lines above the body, joined by '\n'. Our Ferment cards all carry Retain, so it stacks a
/// standalone "Retain." line above "Ferment." — wasting a line. For Ferment cards only, splice the
/// Retain line onto the following (Ferment) line so they read "Retain. Ferment." together.
/// Patches the private 3-arg assembly method both public description getters funnel through.
/// Resolved by name + parameter count so we don't need the internal DescriptionPreviewType at all
/// (referencing it by name/typeof fails: as a typeof it's inaccessible, and AccessTools.TypeByName
/// returned null, which crashed PatchAll and took the whole mod init down with it).
/// </summary>
[HarmonyPatch]
public static class FermentInlineRetainPatch
{
    private static MethodBase TargetMethod() =>
        typeof(CardModel)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .First(m => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);

    private static string RetainTitle => new LocString("card_keywords", "RETAIN.title").GetFormattedText();
    private static string Period => new LocString("card_keywords", "PERIOD").GetRawText();

    // How the game renders Retain: "[gold]Retain[/gold]." — period OUTSIDE the gold tag (white).
    private static string RetainRendered => $"[gold]{RetainTitle}[/gold]{Period}";

    // Our inline form: merged onto the Ferment line, with the period pulled INSIDE the gold.
    private static string RetainInline => $"[gold]{RetainTitle}{Period}[/gold] ";

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is AlchemistCard { IsFermentInline: true } && !string.IsNullOrEmpty(__result))
            __result = __result.Replace(RetainRendered + "\n", RetainInline);
    }
}
