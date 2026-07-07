using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace Alchemist.AlchemistCode.Patches;

// Serve the Brew icon from the mod's own asset tree: at the shared base rest-site path, the mod pck's
// .godot import/uid resolution conflicts and the loader returns null
[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Icon), MethodType.Getter)]
public static class BrewRestSiteIconPatch
{
    private static readonly string BrewIconPath = $"{MainFile.ResPath}/images/rest_site/option_brew.png";
    private static Texture2D? _icon;
    private static bool _tried;

    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance.OptionId != "BREW") return true;
        if (!_tried)
        {
            _tried = true;
            // CacheMode.Ignore forces a fresh load, sidestepping any failure the game cached earlier
            _icon = ResourceLoader.Load<Texture2D>(BrewIconPath, null, ResourceLoader.CacheMode.Ignore);
        }
        if (_icon == null) return true;
        __result = _icon;
        return false;
    }
}
