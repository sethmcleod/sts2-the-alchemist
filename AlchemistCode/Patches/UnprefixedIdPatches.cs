using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// BaseLib adds the "ALCHEMIST-" prefix to our model ids in a postfix on ModelDb.GetEntry, a method one
// call long. A typed lookup such as ModelDb.Power<AntitoxinPower>() reaches GetEntry through GetId, and
// once the JIT inlines GetEntry into a tier-1 caller the postfix does not run for that caller: the
// lookup builds POWER.ANTITOXIN_POWER, misses POWER.ALCHEMIST-ANTITOXIN_POWER and throws. Linux players
// hit it on some launches only. Character select stopped at the starter relic's description (its energy
// icon looks up our relic pool) and combat stopped when the Flask relic granted Antitoxin.
//
// So each of our models is also registered under the id that the unpatched GetEntry builds, and a typed
// lookup finds the same model on both paths. This runs when SaveRenamePatches does, for the same
// reasons: InitIds has already given each model its real id, and the network id cache has already
// numbered the models, so the aliases reach neither
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]
public static class UnprefixedIdPatches
{
    public static void Postfix()
    {
        if (typeof(ModelDb).GetField("_contentById", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null) is not Dictionary<ModelId, AbstractModel> contentById)
        {
            MainFile.Logger.Warn("[UnprefixedIds] ModelDb has no _contentById field in this build of the game, "
                + "so a typed lookup that loses the id prefix still throws.");
            return;
        }

        var ours = Assembly.GetExecutingAssembly();
        var added = 0;
        var taken = new List<string>();
        foreach (var (id, model) in contentById.ToList())
        {
            var type = model.GetType();
            // The model's own entry only. The old ids that SaveRenamePatches adds point at the same models
            if (type.Assembly != ours || id != model.Id) continue;

            var alias = new ModelId(id.Category, StringHelper.Slugify(type.Name));
            if (alias == id) continue;
            // A model that owns the id keeps it. A typed lookup that loses the prefix then gets that
            // model instead of ours, so rename the class
            if (contentById.ContainsKey(alias))
            {
                taken.Add($"{alias} ({type.Name})");
                continue;
            }
            contentById[alias] = model;
            added++;
        }

        MainFile.Logger.Info($"[UnprefixedIds] Registered {added} unprefixed ids as aliases of our models.");
        if (taken.Count > 0)
            MainFile.Logger.Warn($"[UnprefixedIds] These unprefixed ids belong to other models: {string.Join(", ", taken)}.");
    }
}
