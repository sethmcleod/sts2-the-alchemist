using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards;

namespace Alchemist.AlchemistCode.Patches;

// The card description builder that all three patches below postfix. It resolves by name and parameter
// count to avoid naming the internal DescriptionPreviewType, which a typeof cannot reach
internal static class CardDescriptionMethod
{
    internal static MethodBase Resolve() =>
        typeof(CardModel)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .First(m => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);
}

// Join the standalone "Retain." line of a Ferment card to the next line
[HarmonyPatch]
public static class FermentInlineRetainPatch
{
    private static MethodBase TargetMethod() => CardDescriptionMethod.Resolve();

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

// The game prints an enchantment's extra text under the description. The Laced keyword prints above
// it, so the enchantment's purple line moves to the top to read the same way on either card
[HarmonyPatch]
public static class LacedEnchantmentLinePatch
{
    private static MethodBase TargetMethod() => CardDescriptionMethod.Resolve();

    private static string LacedTitle => new LocString("enchantments", "ALCHEMIST-LACED.title").GetFormattedText();

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance.Enchantment is not Enchantments.Laced || string.IsNullOrEmpty(__result)) return;
        var lines = __result.Split('\n').ToList();
        var title = LacedTitle;
        var index = lines.FindIndex(l => l.StartsWith("[purple]") && l.Contains(title));
        if (index <= 0) return;
        var line = lines[index];
        lines.RemoveAt(index);
        lines.Insert(0, line);
        __result = string.Join('\n', lines);
    }
}

// The game prints an automatic keyword as "[gold]Word[/gold]." with a white period. Ferment cards write
// their keyword line with the period inside the tag, so Laced follows them
[HarmonyPatch]
public static class LacedKeywordPeriodPatch
{
    private static MethodBase TargetMethod() => CardDescriptionMethod.Resolve();

    private static string Period => new LocString("card_keywords", "PERIOD").GetRawText();

    private static string LacedTitle => new LocString("card_keywords", "ALCHEMIST-LACED.title").GetFormattedText();

    private static string Rendered => $"[gold]{LacedTitle}[/gold]{Period}";

    private static string Inline => $"[gold]{LacedTitle}{Period}[/gold]";

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (!__instance.Keywords.Contains(AlchemistKeywords.Laced) || string.IsNullOrEmpty(__result)) return;
        __result = __result.Replace(Rendered, Inline);
    }
}
