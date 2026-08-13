using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Alchemist.AlchemistCode.Patches;

// Mend targets an ally by hovering their rest site character, which only works while that character's
// %Hitbox takes mouse input and covers the art. A mod scene owns those two properties, so one bad edit
// or a scene that fails to reimport makes the Alchemist unmendable in multiplayer, with nothing on
// screen to show why. Enforcing both at run time keeps Mend working whatever the packed scene says
[HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
public static class RestSiteHitboxPatch
{
    // The art box from AlchemistRestSite, used only if the scene ships a collapsed Hitbox
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

        // THE Mend fix. A base game rest site scene ships its own SelectionReticle, set to ignore the
        // mouse. A mod scene is told to leave it out, so BaseLib builds one from the Hitbox through
        // CopyControlProperties, which copies MouseFilter as well, and AddChilds it to the root, after
        // ControlRoot. Godot picks controls last child first, so that copy sits over the Hitbox and eats
        // the hover, and Mend can never target this character. The reticle is decoration, so Ignore is
        // always right for it
        if (__instance.GetNodeOrNull<Control>("%SelectionReticle") is { } reticle
            && reticle.MouseFilter != Control.MouseFilterEnum.Ignore)
        {
            reticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            repaired += " reticle mouse filter,";
        }

        // Logged either way: in a multiplayer test this line is the fastest way to tell a targeting
        // problem in the scene from one in the base game's targeting
        MainFile.Logger.Info(
            $"Alchemist rest site hitbox: pos {hitbox.Position}, size {hitbox.Size}, "
            + $"filter {hitbox.MouseFilter}, repaired:{(repaired.Length > 0 ? repaired.TrimEnd(',') : " nothing")}");
    }
}

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
            // CacheMode.Ignore forces a fresh load and ignores any failure that the game cached earlier
            _icon = ResourceLoader.Load<Texture2D>(BrewIconPath, null, ResourceLoader.CacheMode.Ignore);
        }
        if (_icon == null) return true;
        __result = _icon;
        return false;
    }
}
