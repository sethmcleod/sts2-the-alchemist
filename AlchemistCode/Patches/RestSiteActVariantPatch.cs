using Alchemist.AlchemistCode.Compat;
using Alchemist.AlchemistCode.Character;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Alchemist.AlchemistCode.Patches;

// The base loop follows the act index, but act 1 holds the Hive and Glory in either order
// Runs after _Ready queues its own choice, so this one applies later and wins
[HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
public static class RestSiteActVariantPatch
{
    // Unseeded on purpose, so it never draws from the run's seed
    private static readonly Random Rng = new();

    public static void Postfix(NRestSiteCharacter __instance)
    {
        if (__instance.Player?.Character is not Character.Alchemist) return;

        var act = __instance.Player.RunState.Act;
        if (AlchemistRestSite.LoopFor(act) is not { } animation) return;

        foreach (var child in __instance.GetChildren())
        {
            if (child is not Node2D node || node.GetClass() != SpineModel.SpriteClass) continue;

            var sprite = new MegaSprite(node);
            __instance.RunWhenSpineReady(sprite, state =>
            {
                state.SetAnimation(animation, loop: true);

                GameCompat.RandomiseTrackStart(state, Rng);
            });
        }

        MainFile.Logger.Info($"The Alchemist rest site plays {animation} for {act.Id.Entry}.");
    }
}
