using System.Collections.Generic;
using System.Reflection;
using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Relics;
using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Cards.Common;
using Alchemist.AlchemistCode.Cards.Rare;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Cards.Uncommon;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// A saved run and the Run History outlive a release: a card, relic or potion carrying a renamed id
// would load as the game's blank Deprecated model, which is how "continue" after an update lost
// cards to renames and how the Run History shows a NOPE icon for the old starter relics. Every
// rename ships an entry here, mapping the old id to the model that replaced it.
//
// The old ids register as aliases in ModelDb once it is populated, so every lookup resolves them:
// live saves, the Run History, the progress file's discovered lists, Touch of Orobas's remembered
// starter. An earlier version prefixed SaveUtil.CardOrDeprecated and its siblings instead. Those
// methods are a few instructions long, and once a hot caller such as CardModel.FromSave is
// recompiled at tier 1 the JIT inlines them, so the prefix ran for cold callers and not hot ones:
// the Run History header counted a renamed card under its new rarity while the card list under it
// showed Deprecated Card
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]
public static class SaveRenamePatches
{
    // Old entry -> replacement. Built on demand: ModelDb is not populated when Harmony applies the patch
    private static Dictionary<string, ModelId> Cards => new()
    {
        ["ALCHEMIST-STURDY_MIX"] = ModelDb.Card<SyrupyMix>().Id!,
        ["ALCHEMIST-LOB"] = ModelDb.Card<Mash>().Id!,
        ["ALCHEMIST-DOUBLE_BATCH"] = ModelDb.Card<Corrode>().Id!,
        ["ALCHEMIST-PAYS_OFF"] = ModelDb.Card<SmellingSalts>().Id!,
        ["ALCHEMIST-NEXT_UP"] = ModelDb.Card<Spike>().Id!,
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
        ["ALCHEMIST-TOXIN_SKIN"] = ModelDb.Card<Uncork>().Id!,
        ["ALCHEMIST-VIAL_IN_RESERVE"] = ModelDb.Card<Uncork>().Id!,
        // Cuts, not renames: each removed card maps to the new card in its slot, so a mid-save
        // update hands the player something new instead of a blank deprecated card
        ["ALCHEMIST-DOUBLE_DOSE"] = ModelDb.Card<Fumigate>().Id!,
        ["ALCHEMIST-QUICKLIME"] = ModelDb.Card<Spores>().Id!,
        ["ALCHEMIST-ADAPT"] = ModelDb.Card<Vent>().Id!,
        ["ALCHEMIST-LICK"] = ModelDb.Card<Harvest>().Id!,
        ["ALCHEMIST-RETCH"] = ModelDb.Card<Distill>().Id!,
        ["ALCHEMIST-CONGEAL"] = ModelDb.Card<Proof>().Id!,
        ["ALCHEMIST-STIR"] = ModelDb.Card<Corrode>().Id!,
        ["ALCHEMIST-ICHOR"] = ModelDb.Card<Wallop>().Id!,
        ["ALCHEMIST-ALEMBIC"] = ModelDb.Card<Untended>().Id!,
        ["ALCHEMIST-SPEW"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-TOLERANCE"] = ModelDb.Card<WarmUp>().Id!,
        ["ALCHEMIST-CONDENSE"] = ModelDb.Card<Fizz>().Id!,
        ["ALCHEMIST-TWIST"] = ModelDb.Card<Fizz>().Id!,
        ["ALCHEMIST-PORTION"] = ModelDb.Card<Fizz>().Id!,
        ["ALCHEMIST-SMOKE_OUT"] = ModelDb.Card<Digest>().Id!,
        ["ALCHEMIST-SIPHON"] = ModelDb.Card<Dose>().Id!,
        ["ALCHEMIST-PELT"] = ModelDb.Card<Combine>().Id!,
        ["ALCHEMIST-SALVE"] = ModelDb.Card<Tincture>().Id!,
        ["ALCHEMIST-FLARE_UP"] = ModelDb.Card<Endure>().Id!,
        ["ALCHEMIST-KNEAD"] = ModelDb.Card<Endure>().Id!,
        ["ALCHEMIST-HARDEN"] = ModelDb.Card<FreshBatch>().Id!,
        ["ALCHEMIST-ANOINT"] = ModelDb.Card<Spike>().Id!,
        ["ALCHEMIST-LACQUER"] = ModelDb.Card<Brine>().Id!,
        ["ALCHEMIST-RENNET"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-TAP_THE_CASK"] = ModelDb.Card<Harvest>().Id!,
        ["ALCHEMIST-INURE"] = ModelDb.Card<Clench>().Id!,
        ["ALCHEMIST-SWIG"] = ModelDb.Card<Clench>().Id!,
        ["ALCHEMIST-OVERSPILL"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-SEEP"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-DRENCH"] = ModelDb.Card<Harvest>().Id!,
        ["ALCHEMIST-CURE"] = ModelDb.Card<Tincture>().Id!,
        ["ALCHEMIST-CELLAR"] = ModelDb.Card<Ripening>().Id!,
        ["ALCHEMIST-PASS_IT_ON"] = ModelDb.Card<Steep>().Id!,
        ["ALCHEMIST-WRING"] = ModelDb.Card<Tincture>().Id!,
    };

    private static Dictionary<string, ModelId> Potions => new()
    {
        ["ALCHEMIST-QUICKSILVER_DRAUGHT"] = ModelDb.Potion<Potions.VolatileReagent>().Id!,
        ["ALCHEMIST-OLEANDER_MILK"] = ModelDb.Potion<Potions.VolatileReagent>().Id!,
        ["ALCHEMIST-DECOCTION"] = ModelDb.Potion<Potions.Reduction>().Id!,
        ["ALCHEMIST-GOLD_LEAF"] = ModelDb.Potion<Potions.Solvent>().Id!,
    };

    private static Dictionary<string, ModelId> Relics => new()
    {
        ["ALCHEMIST-WEATHERED_KIT"] = ModelDb.Relic<MurkyFlask>().Id!,
        ["ALCHEMIST-GILDED_KIT"] = ModelDb.Relic<RadiantFlask>().Id!,
        // Cuts, not renames: each removed relic maps to the relic that took its slot
        ["ALCHEMIST-SNAKE_TAIL"] = ModelDb.Relic<Bitterroot>().Id!,
        ["ALCHEMIST-SPARE_DOSE"] = ModelDb.Relic<ExtraDose>().Id!,
        ["ALCHEMIST-MIDAS_FRUIT"] = ModelDb.Relic<GlowingShard>().Id!,
        ["ALCHEMIST-AURIC_SEAL"] = ModelDb.Relic<GoldenLeaf>().Id!,
    };

    // InitIds runs once at startup, after ModelDb.Init has registered every model (mod models
    // included) and after ModelIdSerializationCache has numbered them. Registering the aliases
    // here keeps them out of both: InitIds would otherwise stamp a model with whichever of its
    // ids it enumerated last, and the network cache would count each aliased model twice
    public static void Postfix()
    {
        if (typeof(ModelDb).GetField("_contentById", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null) is not Dictionary<ModelId, AbstractModel> contentById)
        {
            MainFile.Logger.Warn("[SaveRename] ModelDb has no _contentById field in this build of the game, "
                + "so renamed cards, relics and potions in older saves load as deprecated.");
            return;
        }

        int added = 0;
        foreach (var table in new[] { Cards, Potions, Relics })
        {
            foreach (var (oldEntry, replacement) in table)
            {
                var alias = new ModelId(replacement.Category, oldEntry);
                // A live model that owns the id wins. The alias is only for ids nothing answers to
                if (contentById.ContainsKey(alias)) continue;
                contentById[alias] = ModelDb.GetById<AbstractModel>(replacement);
                added++;
            }
        }
        MainFile.Logger.Info($"[SaveRename] Registered {added} retired ids as aliases of their replacements.");
    }
}
