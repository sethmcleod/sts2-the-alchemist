using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// The base game loads a rest option's icon from res://images/ui/rest_site/option_{id}.png via
/// PreloadManager. Our Brew option ("BREW") has no such base-game asset. Shipping the icon at that
/// shared base path fails: when the mod pck mounts over the base game, that directory's .godot
/// import / UID resolution conflicts, so both PreloadManager and ResourceLoader return null and
/// get_Icon throws — leaving the Brew button rendered as whatever occupied the slot before (Dig).
///
/// Fix: serve the Brew icon from the mod's OWN asset tree (res://Alchemist/images/rest_site/),
/// which has no path conflict and loads like every other mod icon. Cache it in a static field so
/// we load exactly once and never depend on a (potentially poisoned) shared cache.
/// </summary>
[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Icon), MethodType.Getter)]
public static class BrewRestSiteIconPatch
{
    private static readonly string BrewIconPath = $"{MainFile.ResPath}/images/rest_site/option_brew.png";
    private static Texture2D? _icon;
    private static bool _tried;

    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance.OptionId != "BREW") return true; // only handle our option
        if (!_tried)
        {
            _tried = true;
            // CacheMode.Ignore forces a fresh load, sidestepping any failure the game cached earlier.
            _icon = ResourceLoader.Load<Texture2D>(BrewIconPath, null, ResourceLoader.CacheMode.Ignore);
        }
        if (_icon == null) return true; // fall back to the original loader if somehow unavailable
        __result = _icon;
        return false;
    }
}
