using Alchemist.AlchemistCode.Potions;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace Alchemist.AlchemistCode.Patches;

// Keeps the Unstable look on the belt in sync with the mark, and gives the sweeper a way to reach the
// belt node it needs to shake and burst.
public static class UnstablePotionVfxPatch
{
    private static readonly AccessTools.FieldRef<NPotionContainer, List<NPotionHolder>> HoldersRef =
        AccessTools.FieldRefAccess<NPotionContainer, List<NPotionHolder>>("_holders");

    // The belt rebuilds its potion nodes on any change, so the ambient look has to be reapplied rather
    // than set once at Mark time
    [HarmonyPatch(typeof(NPotion), "Reload")]
    public static class SyncAmbient
    {
        public static void Postfix(NPotion __instance)
        {
            if (!__instance.IsNodeReady()) return;
            Sync(__instance);
        }
    }

    public static void Sync(NPotion node)
    {
        var model = node.Model;
        if (model != null && model.IsMutable && UnstablePotions.IsUnstable(model))
            UnstablePotionVfx.Attach(node);
        else
            UnstablePotionVfx.Detach(node);
    }

    // Only the local player's belt exists on this client, so a potion belonging to anyone else has no
    // node to find. Returns null rather than throwing so every caller can stay unconditional
    public static NPotion? FindBeltNode(PotionModel potion)
    {
        if (!LocalContext.IsMine(potion)) return null;
        if (NRun.Instance?.GlobalUi.TopBar.PotionContainer is not { } container) return null;

        foreach (var holder in HoldersRef(container))
            if (holder.Potion is { } node && node.Model == potion)
                return node;

        return null;
    }

    public static void Refresh(PotionModel potion)
    {
        if (FindBeltNode(potion) is { } node && node.IsNodeReady())
            Sync(node);
    }

    public static void Shake(PotionModel potion)
    {
        if (FindBeltNode(potion) is { } node) UnstablePotionVfx.Shake(node);
    }

    // The burst is parented to the holder rather than the potion, so it survives the potion node being
    // hidden and keeps playing where the potion used to sit. It is placed on the potion's centre in the
    // holder's space, not the holder's origin, or it fires from the corner of the belt
    public static void Burst(PotionModel potion)
    {
        if (FindBeltNode(potion) is not { } node) return;
        if (node.GetParent() is not NPotionHolder holder) return;

        UnstablePotionVfx.PlayBurst(holder, UnstablePotionVfx.Center(node));
        node.Visible = false;
    }
}
