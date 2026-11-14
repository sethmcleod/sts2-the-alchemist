using MegaCrit.Sts2.Core.Localization.DynamicVars;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Cards;

public class HitCountVar(int hits) : DynamicVar("HitCount", hits);

public static class HitCountVarExtensions
{
    extension(DynamicVarSet vars)
    {
        public HitCountVar HitCount => (HitCountVar)vars["HitCount"];
    }
}
