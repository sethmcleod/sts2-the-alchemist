using BaseLib.Utils.ModInterop;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

[ModInterop(AncientsAwakenedMod.ModId, AncientsAwakenedMod.PerfectedRegistry)]
internal static class AncientsAwakenedPerfectedInterop
{
    public static void AddPerfectedCardForCustomCharacters(CardModel cardModel, CardModel originalCardModel)
    {
    }

    public static CardPoolModel? PerfectedCardPool(CardPoolModel? optionalFallBack)
    {
        return optionalFallBack;
    }
}
