using BaseLib.Utils.ModInterop;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[ModInterop("ReshuffledSpire", Registry)]
internal static class ReshuffledSpireMod
{
    private const string Registry = "ReshuffledSpire.ReshuffledSpireCode.Extensions.AncientVariantExtension";

    private static bool? _loaded;

    internal static bool IsLoaded => _loaded ??= AccessTools.TypeByName(Registry) != null;

    public static void RegisterAncientVariant(ModelId baseCardId, ModelId ancientVariantId)
    {
    }
}
