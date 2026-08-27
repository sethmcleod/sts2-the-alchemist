using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Relics;
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
        ["ALCHEMIST-PAYS_OFF"] = ModelDb.Card<SmellingSalts>().Id!,
        ["ALCHEMIST-NEXT_UP"] = ModelDb.Card<Anoint>().Id!,
        ["ALCHEMIST-FRESH_COAT"] = ModelDb.Card<Untended>().Id!,
        ["ALCHEMIST-ELIXIR"] = ModelDb.Card<Panacea>().Id!,
        ["ALCHEMIST-ANTIDOTE"] = ModelDb.Card<Dose>().Id!,
        ["ALCHEMIST-DEEP_CUT"] = ModelDb.Card<Bonk>().Id!,
        ["ALCHEMIST-REAGENT"] = ModelDb.Card<Reclaim>().Id!,
        ["ALCHEMIST-WHITE_HEAT"] = ModelDb.Card<WaterDown>().Id!,
        ["ALCHEMIST-RIPEN"] = ModelDb.Card<Rerun>().Id!,
        ["ALCHEMIST-SIMMER"] = ModelDb.Card<Runoff>().Id!,
        ["ALCHEMIST-QUAFF"] = ModelDb.Card<Meltdown>().Id!,
        ["ALCHEMIST-IMMUNIZE"] = ModelDb.Card<Mellow>().Id!,
        ["ALCHEMIST-POULTICE"] = ModelDb.Card<Upwell>().Id!,
        ["ALCHEMIST-SLOW_BURN"] = ModelDb.Card<Mortar>().Id!,
        ["ALCHEMIST-SWILL"] = ModelDb.Card<TasteTest>().Id!,
        ["ALCHEMIST-STEEP"] = ModelDb.Card<PourOver>().Id!,
        ["ALCHEMIST-TOXIN_SKIN"] = ModelDb.Card<VialInReserve>().Id!,
        // Cuts, not renames: each retired card maps to the new card in its slot, so a mid-save
        // update hands the player something new instead of a blank deprecated card
        ["ALCHEMIST-DOUBLE_DOSE"] = ModelDb.Card<Fumigate>().Id!,
        ["ALCHEMIST-QUICKLIME"] = ModelDb.Card<Spores>().Id!,
        ["ALCHEMIST-ADAPT"] = ModelDb.Card<Vent>().Id!,
        ["ALCHEMIST-LICK"] = ModelDb.Card<Drench>().Id!,
        ["ALCHEMIST-RETCH"] = ModelDb.Card<Distill>().Id!,
        ["ALCHEMIST-CONGEAL"] = ModelDb.Card<Proof>().Id!,
        ["ALCHEMIST-STIR"] = ModelDb.Card<FreshBatch>().Id!,
        ["ALCHEMIST-ICHOR"] = ModelDb.Card<Wallop>().Id!,
        ["ALCHEMIST-ALEMBIC"] = ModelDb.Card<Untended>().Id!,
    };

    public static void Prefix(ref ModelId id)
    {
        if (id?.Entry != null && Renamed.TryGetValue(id.Entry, out var replacement))
            id = replacement;
    }
}

// The potion half: a renamed potion id sitting in a saved belt loads as a blank
// DeprecatedPotion without this map
[HarmonyPatch(typeof(SaveUtil), nameof(SaveUtil.PotionOrDeprecated))]
public static class PotionSaveRenamePatches
{
    private static Dictionary<string, ModelId>? _renamed;

    // Lazy: ModelDb is not populated when Harmony applies the patch
    private static Dictionary<string, ModelId> Renamed => _renamed ??= new Dictionary<string, ModelId>
    {
        ["ALCHEMIST-QUICKSILVER_DRAUGHT"] = ModelDb.Potion<Potions.OleanderMilk>().Id!,
    };

    public static void Prefix(ref ModelId id)
    {
        if (id?.Entry != null && Renamed.TryGetValue(id.Entry, out var replacement))
            id = replacement;
    }
}

// The relic half of the same problem: RelicModel.FromSave resolves through
// SaveUtil.RelicOrDeprecated, so a retired relic id in a live save loads as a blank DeprecatedRelic
[HarmonyPatch(typeof(SaveUtil), nameof(SaveUtil.RelicOrDeprecated))]
public static class RelicSaveRenamePatches
{
    private static Dictionary<string, ModelId>? _renamed;

    // Lazy: ModelDb is not populated when Harmony applies the patch
    private static Dictionary<string, ModelId> Renamed => _renamed ??= new Dictionary<string, ModelId>
    {
        // Cuts, not renames: each retired relic maps to the relic that took its slot
        ["ALCHEMIST-SNAKE_TAIL"] = ModelDb.Relic<Bitterroot>().Id!,
        ["ALCHEMIST-SPARE_DOSE"] = ModelDb.Relic<ExtraDose>().Id!,
        ["ALCHEMIST-GLOWING_SHARD"] = ModelDb.Relic<MotherOfVinegar>().Id!,
    };

    public static void Prefix(ref ModelId id)
    {
        if (id?.Entry != null && Renamed.TryGetValue(id.Entry, out var replacement))
            id = replacement;
    }
}
