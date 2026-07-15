using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Alchemist.AlchemistCode.Config;
using AlchemistCharacter = Alchemist.AlchemistCode.Character.Alchemist;
using AlchemistCards = Alchemist.AlchemistCode.Character.AlchemistCardPool;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Patches;

// Keeps Alchemist cards out of runs that don't include an Alchemist player, so mod content can't leak into
// other characters' cross-class rewards (Kaleidoscope, Splash, Colorful Philosophers, Prismatic Gem — all
// draw from UnlockState.CharacterCardPools). Toggleable via mod config; character select / card library use
// UnlockState.Characters instead, so the Alchemist stays visible there regardless.
[HarmonyPatch]
public static class PoolPatches
{
    // Only strip inside an active run that has no Alchemist player. Outside a run we leave everything alone
    // so menus and the compendium are never affected.
    private static bool ShouldStrip()
    {
        if (!AlchemistModConfig.KeepPoolsSeparate) return false;
        var state = RunManager.Instance.DebugOnlyGetState();
        if (state == null) return false;
        return !state.Players.Any(p => p.Character is AlchemistCharacter);
    }

    [HarmonyPatch(typeof(UnlockState), "CharacterCardPools", MethodType.Getter)]
    [HarmonyPostfix]
    private static void StripAlchemistPool(ref IEnumerable<CardPoolModel> __result)
    {
        if (!ShouldStrip()) return;
        __result = __result.Where(p => p is not AlchemistCards).ToList();
    }

    // Kaleidoscope is offered at Neow only when you've unlocked every character's card pool
    // (CharacterCardPools.Count() == AllCharacters.Count()). Since we removed our always-unlocked pool from
    // that set, base characters would fall one short and lose Kaleidoscope — recheck against the non-Alchemist count.
    [HarmonyPatch(typeof(Kaleidoscope), nameof(Kaleidoscope.IsAllowedAtNeow))]
    [HarmonyPostfix]
    private static void FixKaleidoscopeNeow(Player player, ref bool __result)
    {
        if (__result || !ShouldStrip()) return;
        var required = ModelDb.AllCharacters.Count(c => c is not AlchemistCharacter);
        __result = player.UnlockState.CharacterCardPools.Count() == required;
    }
}
