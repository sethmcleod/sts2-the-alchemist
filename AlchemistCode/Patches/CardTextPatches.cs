using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards;

namespace Alchemist.AlchemistCode.Patches;

// Resolved by name and parameter count, since typeof can't reach DescriptionPreviewType
internal static class CardDescriptionMethod
{
    internal static MethodBase Resolve() =>
        typeof(CardModel)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .First(m => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);
}

[HarmonyPatch]
public static class FermentInlineRetainPatch
{
    private static MethodBase TargetMethod() => CardDescriptionMethod.Resolve();

    private static string RetainTitle => new LocString("card_keywords", "RETAIN.title").GetFormattedText();
    private static string Period => new LocString("card_keywords", "PERIOD").GetRawText();

    // Must match the game's output, period outside the gold tag
    private static string RetainRendered => $"[gold]{RetainTitle}[/gold]{Period}";

    private static string RetainInline => $"[gold]{RetainTitle}{Period}[/gold] ";

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is AlchemistCard { IsFermentInline: true } && !string.IsNullOrEmpty(__result))
            __result = __result.Replace(RetainRendered + "\n", RetainInline);
    }
}

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
