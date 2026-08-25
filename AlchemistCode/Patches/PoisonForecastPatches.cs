using Alchemist.AlchemistCode.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Patches;

// PoisonPower.CalculateTotalDamageNextTurn sizes the incoming-damage preview by running the real
// ModifyDamage hook once per Poison trigger. Those passes reach AntitoxinPower.Absorb wearing the exact
// signature of a genuine tick, but they never reach BeforeDamageReceived, so anything Absorb records is
// left behind for whatever damage lands next. Marking the forecast lets Absorb reduce the previewed
// number, which is what keeps the preview honest, while leaving the pending spend alone.
//
// Finalizer rather than Postfix, so the flag still clears if the forecast throws
[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.CalculateTotalDamageNextTurn))]
public static class PoisonForecastScopePatch
{
    public static void Prefix() => AntitoxinRules.InPoisonForecast = true;

    public static void Finalizer() => AntitoxinRules.InPoisonForecast = false;
}
