using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Cards.ReshuffledSpire;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class ReshuffledSpirePatches
{
    public static void Postfix()
    {
        if (!ReshuffledSpireMod.IsLoaded) return;
        ReshuffledSpireMod.RegisterAncientVariant(ModelDb.GetId<Dose>(), ModelDb.GetId<HeroicDose>());
        MainFile.Logger.Info("Registered Heroic Dose with Reshuffled Spire as the Gilded Tooth variant of Dose");
    }
}
