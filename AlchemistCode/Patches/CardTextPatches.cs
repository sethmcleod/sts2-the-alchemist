using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards;

namespace Alchemist.AlchemistCode.Patches;

// Splice a Ferment card's standalone "Retain." line onto the following Ferment line. Resolved by name +
// param count so we never reference the internal DescriptionPreviewType (inaccessible as a typeof)
[HarmonyPatch]
public static class FermentInlineRetainPatch
{
    private static MethodBase TargetMethod() =>
        typeof(CardModel)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .First(m => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);

    private static string RetainTitle => new LocString("card_keywords", "RETAIN.title").GetFormattedText();
    private static string Period => new LocString("card_keywords", "PERIOD").GetRawText();

    // The game renders Retain with the period outside the gold tag; our inline form pulls it inside
    private static string RetainRendered => $"[gold]{RetainTitle}[/gold]{Period}";

    private static string RetainInline => $"[gold]{RetainTitle}{Period}[/gold] ";

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is AlchemistCard { IsFermentInline: true } && !string.IsNullOrEmpty(__result))
            __result = __result.Replace(RetainRendered + "\n", RetainInline);
    }
}
