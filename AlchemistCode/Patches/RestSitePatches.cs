using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Alchemist.AlchemistCode.Patches;

// Mend targets an ally through %Hitbox, so its mouse input and size are enforced at run time
[HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
public static class RestSiteHitboxPatch
{
    // Must match the art box in AlchemistRestSite
    private static readonly Vector2 FallbackTopLeft = new(-196f, -267f);
    private static readonly Vector2 FallbackSize = new(392f, 519f);

    public static void Postfix(NRestSiteCharacter __instance)
    {
        if (__instance.Player?.Character is not Character.Alchemist) return;
        if (__instance.Hitbox is not { } hitbox) return;

        var repaired = "";
        if (hitbox.MouseFilter != Control.MouseFilterEnum.Stop)
        {
            hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
            repaired += " mouse filter,";
        }
        if (hitbox.Size.X < 1f || hitbox.Size.Y < 1f)
        {
            hitbox.Position = FallbackTopLeft;
            hitbox.Size = FallbackSize;
            repaired += " size,";
        }

        // BaseLib's copied SelectionReticle sits over the Hitbox and eats the hover Mend needs
        if (__instance.GetNodeOrNull<Control>("%SelectionReticle") is { } reticle
            && reticle.MouseFilter != Control.MouseFilterEnum.Ignore)
        {
            reticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            repaired += " reticle mouse filter,";
        }

        MainFile.Logger.Info(
            $"Alchemist rest site hitbox: pos {hitbox.Position}, size {hitbox.Size}, "
            + $"filter {hitbox.MouseFilter}, repaired:{(repaired.Length > 0 ? repaired.TrimEnd(',') : " nothing")}");
    }
}

// At the base rest-site path, the mod pck's import resolution conflicts and the load returns null
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
            _icon = ResourceLoader.Load<Texture2D>(BrewIconPath, null, ResourceLoader.CacheMode.Ignore);
        }
        if (_icon == null) return true;
        __result = _icon;
        return false;
    }
}
