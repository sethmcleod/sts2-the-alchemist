using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class UntendedPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef };

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card,
        bool fromHandDraw)
    {
        if (card.Owner?.Creature != Owner) return;
        if (card is not AlchemistCard { IsFermentInline: true } ferment) return;
        Flash();
        await ferment.AdvanceFerment((int)Amount);
    }
}
