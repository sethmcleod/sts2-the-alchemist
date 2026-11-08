using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace Alchemist.AlchemistCode.Patches;

// GetImagePath prepends res://images/ to our absolute icon path, so run history badges load blank
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

            // Ignore skips the failed load the mangled path already cached
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
