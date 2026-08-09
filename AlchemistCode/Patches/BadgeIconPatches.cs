using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace Alchemist.AlchemistCode.Patches;

// The run history screen rebuilds a badge from its saved id. BaseLib swaps the custom icon path in before
// NBadge.Create passes it to ImageHelper.GetImagePath, which prepends res://images/ to a path that is
// already absolute, so the icon loads as res://images/res://Alchemist/... and renders blank. The game over
// screen escapes this because it reads Badge.IconPath, which BaseLib serves raw. Set the texture after
// Create returns, from the same CustomBadgeIconPath the badge class already declares
[HarmonyPatch(typeof(NBadge), nameof(NBadge.Create), typeof(string), typeof(BadgeRarity))]
public static class BadgeIconPatches
{
    private static Dictionary<string, Texture2D?>? _icons;

    private static Dictionary<string, Texture2D?> Icons()
    {
        if (_icons != null) return _icons;

        _icons = new Dictionary<string, Texture2D?>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            if (type.IsAbstract || !typeof(CustomBadge).IsAssignableFrom(type)) continue;

            var badge = (CustomBadge)Activator.CreateInstance(type)!;
            var path = badge.CustomBadgeIconPath;
            if (string.IsNullOrEmpty(path)) continue;

            // CacheMode.Ignore forces a fresh load past the failure the mangled path already cached
            _icons[badge.Id] = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Ignore);
        }
        return _icons;
    }

    public static void Postfix(string id, NBadge? __result)
    {
        if (__result == null) return;
        if (!Icons().TryGetValue(id, out var icon) || icon == null) return;
        __result.GetNode<TextureRect>("%Icon").Texture = icon;
    }
}
