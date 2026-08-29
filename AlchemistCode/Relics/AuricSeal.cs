using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Relics;

public class AuricSeal : AlchemistRelic
{
    private const int FillPerDraw = 1;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Decant) };

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card,
        bool fromHandDraw)
    {
        if (card.Owner != Owner) return Task.CompletedTask;
        if (card is not AlchemistCard { IsDecantCard: true } decant) return Task.CompletedTask;
        Flash();
        decant.AddDecant(FillPerDraw);
        return Task.CompletedTask;
    }
}
