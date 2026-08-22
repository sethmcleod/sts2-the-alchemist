using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Cards.Common;
using Alchemist.AlchemistCode.Cards.Rare;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Cards.Uncommon;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Patches;

// A saved run outlives a release: a deck card carrying a renamed id would load as the game's
// blank DeprecatedCard, which is how "continue" after an update lost cards to renames. Every
// rename ships an entry here, mapping the retired id to the card that replaced it
[HarmonyPatch(typeof(SaveUtil), nameof(SaveUtil.CardOrDeprecated))]
public static class SaveRenamePatches
{
    private static Dictionary<string, ModelId>? _renamed;

    // Lazy: ModelDb is not populated when Harmony applies the patch
    private static Dictionary<string, ModelId> Renamed => _renamed ??= new Dictionary<string, ModelId>
    {
        ["ALCHEMIST-STURDY_MIX"] = ModelDb.Card<SyrupyMix>().Id!,
        ["ALCHEMIST-LOB"] = ModelDb.Card<Mash>().Id!,
        ["ALCHEMIST-DOUBLE_BATCH"] = ModelDb.Card<FreshBatch>().Id!,
        ["ALCHEMIST-DREGS"] = ModelDb.Card<Residue>().Id!,
        ["ALCHEMIST-PURGE"] = ModelDb.Card<DoubleDose>().Id!,
        ["ALCHEMIST-PAYS_OFF"] = ModelDb.Card<SmellingSalts>().Id!,
        ["ALCHEMIST-NEXT_UP"] = ModelDb.Card<Anoint>().Id!,
        ["ALCHEMIST-FRESH_COAT"] = ModelDb.Card<Alembic>().Id!,
        ["ALCHEMIST-ELIXIR"] = ModelDb.Card<Panacea>().Id!,
        ["ALCHEMIST-ANTIDOTE"] = ModelDb.Card<Dose>().Id!,
    };

    public static void Prefix(ref ModelId id)
    {
        if (id?.Entry != null && Renamed.TryGetValue(id.Entry, out var replacement))
            id = replacement;
    }
}
