using System;
using Alchemist.AlchemistCode.Cards.AncientsAwakened;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class AncientsAwakenedPatches
{
    public static void Postfix()
    {
        try
        {
            AncientsAwakenedMod.Register();
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Ancients Awakened registration failed, so its relics fall back to their generic cards: {e}");
        }
    }
}
