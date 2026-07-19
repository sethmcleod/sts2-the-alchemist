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

// This keeps Alchemist cards out of a run that has no Alchemist player. The mod content then cannot
// appear in the cross-class rewards of another character. Kaleidoscope, Splash, Colorful Philosophers,
// and Prismatic Gem all read UnlockState.CharacterCardPools. The mod config can turn this off. The
// character select screen and the card library read UnlockState.Characters, so the Alchemist stays
// visible there
[HarmonyPatch]
public static class PoolPatches
{
    // Remove the cards only inside an active run that has no Alchemist player. Outside a run, this
    // changes nothing, so the menus and the compendium stay correct
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
    // that set, base characters would fall one short and lose Kaleidoscope, so recheck against the
    // non-Alchemist count.
    [HarmonyPatch(typeof(Kaleidoscope), nameof(Kaleidoscope.IsAllowedAtNeow))]
    [HarmonyPostfix]
    private static void FixKaleidoscopeNeow(Player player, ref bool __result)
    {
        if (__result || !ShouldStrip()) return;
        var required = ModelDb.AllCharacters.Count(c => c is not AlchemistCharacter);
        __result = player.UnlockState.CharacterCardPools.Count() == required;
    }
}
