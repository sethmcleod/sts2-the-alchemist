using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// The Brew rest-site icon is a mod asset at a base-game path
/// (res://images/ui/rest_site/option_brew.png). The base game loads option icons through
/// PreloadManager.Cache, which never preloads mod assets — an intermittent cache miss throws inside
/// get_Icon and aborts NRestSiteRoom.UpdateRestSiteOptions mid-rebuild, so the Brew button ends up
/// showing a stale/other option (e.g. Dig). Load our icon directly via ResourceLoader (which resolves
/// mod assets reliably and uses Godot's own resource cache, not the game's preload cache).
/// </summary>
[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Icon), MethodType.Getter)]
public static class BrewRestSiteIconPatch
{
    private const string BrewIconPath = "res://images/ui/rest_site/option_brew.png";

    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance.OptionId != "BREW") return true; // only handle our option
        var tex = ResourceLoader.Load<Texture2D>(BrewIconPath, null, ResourceLoader.CacheMode.Reuse);
        if (tex == null) return true; // fall back to the original loader if somehow unavailable
        __result = tex;
        return false;
    }
}
