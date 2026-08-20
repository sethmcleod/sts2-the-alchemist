using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;

namespace Alchemist.AlchemistCode.Potions;

// Bestow is the only card that makes a Potion, and it goes through here so the healing blacklist
// applies to it.
public static class Brewing
{
    public static async Task<PotionModel?> Produce(Player player, Rng rng)
    {
        var potion = PotionFactory.CreateRandomPotionInCombat(player, rng, HealingPotions(player));
        if (potion == null) return null;
        var result = await PotionCmd.TryToProcure(potion.ToMutable(), player);
        return result.success ? result.potion : null;
    }

    // CreateRandomPotionInCombat already blocks Fairy in a Bottle, Regen Potion and Fruit Juice, but not
    // Blood Potion or Gold Leaf. Detecting it rather than listing it means a healing potion added later
    // is excluded without anyone remembering this file
    private static IEnumerable<PotionModel> HealingPotions(Player player) =>
        GameCompat.GetPotionOptions(player).Where(IsHealing);

    private static bool IsHealing(PotionModel potion) =>
        potion.DynamicVars.Values.Any(v =>
            v is HealVar or MaxHpVar
            // Blood Potion's heal is a plain DynamicVar named "HealPercent", and Gold Leaf's is a custom
            // subclass, so neither is caught by type alone
            || v.Name.Contains("Heal", System.StringComparison.OrdinalIgnoreCase)
            || v.Name.Contains("MaxHp", System.StringComparison.OrdinalIgnoreCase)
            || IsPowerVarFor<RegenPower>(v));

    private static bool IsPowerVarFor<T>(DynamicVar v) where T : PowerModel
    {
        var t = v.GetType();
        return t.IsGenericType
               && t.GetGenericTypeDefinition() == typeof(PowerVar<>)
               && t.GetGenericArguments()[0] == typeof(T);
    }
}
