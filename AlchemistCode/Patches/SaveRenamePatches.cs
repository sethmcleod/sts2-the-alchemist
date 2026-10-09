using System.Collections.Generic;
using System.Reflection;
using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Relics;
using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Cards.Common;
using Alchemist.AlchemistCode.Cards.Hero;
using Alchemist.AlchemistCode.Cards.Rare;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Cards.Uncommon;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]
public static class SaveRenamePatches
{
    private static Dictionary<string, ModelId> Cards => new()
    {
        ["ALCHEMIST-ADAPT"] = ModelDb.Card<Vent>().Id!,
        ["ALCHEMIST-ALEMBIC"] = ModelDb.Card<Untended>().Id!,
        ["ALCHEMIST-ANOINT"] = ModelDb.Card<Spike>().Id!,
        ["ALCHEMIST-ANTIDOTE"] = ModelDb.Card<Dose>().Id!,
        ["ALCHEMIST-CALLUS"] = ModelDb.Card<CoupDeGrace>().Id!,
        ["ALCHEMIST-CELLAR"] = ModelDb.Card<Ripening>().Id!,
        ["ALCHEMIST-CONDENSE"] = ModelDb.Card<Fizz>().Id!,
        ["ALCHEMIST-CONGEAL"] = ModelDb.Card<Proof>().Id!,
        ["ALCHEMIST-CURE"] = ModelDb.Card<Knitbone>().Id!,
        ["ALCHEMIST-DEEP_CUT"] = ModelDb.Card<Bonk>().Id!,
        ["ALCHEMIST-DOUBLE_BATCH"] = ModelDb.Card<Corrode>().Id!,
        ["ALCHEMIST-DOUBLE_DOSE"] = ModelDb.Card<Fumigate>().Id!,
        ["ALCHEMIST-DRENCH"] = ModelDb.Card<Harvest>().Id!,
        ["ALCHEMIST-ELIXIR"] = ModelDb.Card<Panacea>().Id!,
        ["ALCHEMIST-FALLOUT"] = ModelDb.Card<PickAndChoose>().Id!,
        ["ALCHEMIST-FLARE_UP"] = ModelDb.Card<Endure>().Id!,
        ["ALCHEMIST-FRESH_COAT"] = ModelDb.Card<Untended>().Id!,
        ["ALCHEMIST-HARDEN"] = ModelDb.Card<FreshBatch>().Id!,
        ["ALCHEMIST-ICHOR"] = ModelDb.Card<Wallop>().Id!,
        ["ALCHEMIST-IMMUNIZE"] = ModelDb.Card<Mellow>().Id!,
        ["ALCHEMIST-INURE"] = ModelDb.Card<Clench>().Id!,
        ["ALCHEMIST-KNEAD"] = ModelDb.Card<Endure>().Id!,
        ["ALCHEMIST-LACQUER"] = ModelDb.Card<Brine>().Id!,
        ["ALCHEMIST-LICK"] = ModelDb.Card<Harvest>().Id!,
        ["ALCHEMIST-LOB"] = ModelDb.Card<Mash>().Id!,
        ["ALCHEMIST-MELTDOWN"] = ModelDb.Card<Fling>().Id!,
        ["ALCHEMIST-NEXT_UP"] = ModelDb.Card<Spike>().Id!,
        ["ALCHEMIST-OVERDOSE"] = ModelDb.Card<Premonition>().Id!,
        ["ALCHEMIST-OVERSPILL"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-PASS_IT_ON"] = ModelDb.Card<Steep>().Id!,
        ["ALCHEMIST-PAYS_OFF"] = ModelDb.Card<SmellingSalts>().Id!,
        ["ALCHEMIST-PELT"] = ModelDb.Card<Combine>().Id!,
        ["ALCHEMIST-PORTION"] = ModelDb.Card<Fizz>().Id!,
        ["ALCHEMIST-POULTICE"] = ModelDb.Card<Upwell>().Id!,
        ["ALCHEMIST-QUAFF"] = ModelDb.Card<Fling>().Id!,
        ["ALCHEMIST-QUICKLIME"] = ModelDb.Card<Spores>().Id!,
        ["ALCHEMIST-REAGENT"] = ModelDb.Card<WaitingGame>().Id!,
        ["ALCHEMIST-RECLAIM"] = ModelDb.Card<WaitingGame>().Id!,
        ["ALCHEMIST-REFLUX"] = ModelDb.Card<Preserve>().Id!,
        ["ALCHEMIST-RENNET"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-RETCH"] = ModelDb.Card<Distill>().Id!,
        ["ALCHEMIST-RIPEN"] = ModelDb.Card<Rerun>().Id!,
        ["ALCHEMIST-SALVE"] = ModelDb.Card<Knitbone>().Id!,
        ["ALCHEMIST-SEEP"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-SIMMER"] = ModelDb.Card<Runoff>().Id!,
        ["ALCHEMIST-SIPHON"] = ModelDb.Card<Dose>().Id!,
        ["ALCHEMIST-SLOW_BURN"] = ModelDb.Card<Mortar>().Id!,
        ["ALCHEMIST-SMOKE_OUT"] = ModelDb.Card<Digest>().Id!,
        ["ALCHEMIST-SPEW"] = ModelDb.Card<Overflow>().Id!,
        ["ALCHEMIST-STIR"] = ModelDb.Card<Corrode>().Id!,
        ["ALCHEMIST-STURDY_MIX"] = ModelDb.Card<SyrupyMix>().Id!,
        ["ALCHEMIST-SWIG"] = ModelDb.Card<Clench>().Id!,
        ["ALCHEMIST-SWILL"] = ModelDb.Card<TasteTest>().Id!,
        ["ALCHEMIST-TAP_THE_CASK"] = ModelDb.Card<Harvest>().Id!,
        ["ALCHEMIST-TINCTURE"] = ModelDb.Card<Knitbone>().Id!,
        ["ALCHEMIST-TOLERANCE"] = ModelDb.Card<WarmUp>().Id!,
        ["ALCHEMIST-TOXIN_SKIN"] = ModelDb.Card<Uncork>().Id!,
        ["ALCHEMIST-TWIST"] = ModelDb.Card<Fizz>().Id!,
        ["ALCHEMIST-VIAL_IN_RESERVE"] = ModelDb.Card<Uncork>().Id!,
        ["ALCHEMIST-VITRIFY"] = ModelDb.Card<LayerUp>().Id!,
        ["ALCHEMIST-WHITE_HEAT"] = ModelDb.Card<WaterDown>().Id!,
        ["ALCHEMIST-WRING"] = ModelDb.Card<Knitbone>().Id!,
    };

    private static Dictionary<string, ModelId> Potions => new()
    {
        ["ALCHEMIST-DECOCTION"] = ModelDb.Potion<Potions.Reduction>().Id!,
        ["ALCHEMIST-OLEANDER_MILK"] = ModelDb.Potion<Potions.Quicksilver>().Id!,
        ["ALCHEMIST-QUICKSILVER_DRAUGHT"] = ModelDb.Potion<Potions.Quicksilver>().Id!,
        ["ALCHEMIST-VOLATILE_REAGENT"] = ModelDb.Potion<Potions.Quicksilver>().Id!,
    };

    private static Dictionary<string, ModelId> Relics => new()
    {
        ["ALCHEMIST-AURIC_SEAL"] = ModelDb.Relic<ChimeraBlossom>().Id!,
        ["ALCHEMIST-BITTERROOT"] = ModelDb.Relic<Homunculus>().Id!,
        ["ALCHEMIST-EXTRA_DOSE"] = ModelDb.Relic<Toadstone>().Id!,
        ["ALCHEMIST-GILDED_KIT"] = ModelDb.Relic<RadiantFlask>().Id!,
        ["ALCHEMIST-GOLDEN_LEAF"] = ModelDb.Relic<ChimeraBlossom>().Id!,
        ["ALCHEMIST-MIDAS_FRUIT"] = ModelDb.Relic<GlowingShard>().Id!,
        ["ALCHEMIST-SECOND_SKIN"] = ModelDb.Relic<LilypadCloak>().Id!,
        ["ALCHEMIST-SNAKE_TAIL"] = ModelDb.Relic<Homunculus>().Id!,
        ["ALCHEMIST-SPARE_DOSE"] = ModelDb.Relic<Toadstone>().Id!,
        ["ALCHEMIST-WEATHERED_KIT"] = ModelDb.Relic<MurkyFlask>().Id!,
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
                if (contentById.ContainsKey(alias)) continue;
                contentById[alias] = ModelDb.GetById<AbstractModel>(replacement);
                added++;
            }
        }
        MainFile.Logger.Info($"[SaveRename] Registered {added} retired ids as aliases of their replacements.");
    }
}
