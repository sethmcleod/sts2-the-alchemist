using Alchemist.AlchemistCode.Compat;
using Alchemist.AlchemistCode.Character;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Alchemist.AlchemistCode.Patches;

// NRestSiteCharacter reads the act index alone to pick its loop, thus act 1 always plays
// overgrowth_loop. Act 1 holds two maps though, and the Underdocks is lit blue where the Overgrowth
// is green. The act 3 loop already carries a blue light, thus the Underdocks borrows it.
//
// This runs after _Ready has queued its own choice. Both wait on the same skeleton, and the one
// that registers later applies later, thus this one wins
[HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
public static class RestSiteActVariantPatch
{
    // A private generator, thus the start of the loop never draws from the seeded run of the game
    private static readonly Random Rng = new();

    public static void Postfix(NRestSiteCharacter __instance)
    {
        if (__instance.Player?.Character is not Character.Alchemist) return;
        if (__instance.Player.RunState.Act is not Underdocks) return;
        if (AlchemistRestSite.UnderdocksAnimation is not { } animation) return;

        foreach (var child in __instance.GetChildren())
        {
            if (child is not Node2D node || node.GetClass() != SpineModel.SpriteClass) continue;

            var sprite = new MegaSprite(node);
            __instance.RunWhenSpineReady(sprite, state =>
            {
                state.SetAnimation(animation, loop: true);

                // The base game starts each character at a random point so that two of them on
                // screen never sway together
                GameCompat.RandomiseTrackStart(state, Rng);
            });
        }

        MainFile.Logger.Info($"The Alchemist rest site plays {animation} for the Underdocks.");
    }
}
