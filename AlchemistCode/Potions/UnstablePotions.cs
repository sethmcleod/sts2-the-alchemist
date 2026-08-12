using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

// An Unstable potion is discarded when combat ends. It is what lets a card make a potion without
// feeding the run economy: the potion is real for this fight and gone afterwards.
//
// The mark is held outside the potion rather than on it, because a mutable potion is a plain base-game
// model that the Alchemist cannot subclass. A ConditionalWeakTable keeps no potion alive on its own,
// so a discarded potion is collected as usual. The game does not save mid-combat, so the mark never
// needs to survive a reload.
public static class UnstablePotions
{
    private static readonly ConditionalWeakTable<PotionModel, object> Marks = new();

    public static IHoverTip Tip => AlchemistTips.Static("ALCHEMIST-UNSTABLE");

    public static void Mark(PotionModel potion)
    {
        potion.AssertMutable();
        Marks.GetValue(potion, _ => new object());
    }

    public static bool IsUnstable(PotionModel potion) => Marks.TryGetValue(potion, out _);
}
