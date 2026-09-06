using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

// A played Ferment card keeps its turns, so sending it back into the draw pile instead of the
// discard is a second brew of the same card. The base Rebound power uses this same location hook;
// a card that Exhausts is not headed for the discard and is left alone.
// The hook pair is spelled differently on the two game branches, so the overrides live in
// Compat/SteepPowerCompat.cs
public partial class SteepPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef };

    private bool Steeps(CardModel card) =>
        card.Owner?.Creature == Owner && card is AlchemistCard { IsFermentInline: true };
}
