using System.Collections.Generic;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class OneForLaterPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => Mixing.MixRefTips();

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != Owner.Player || !Mixing.IsMix(card) || card.Pile?.Type != PileType.Hand) return;
        Flash();
        for (var i = 0; i < (int)Amount; i++)
        {
            var copy = card.CreateClone();
            Mixing.RecordCreated(Owner.Player, copy, this);
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner.Player, CardPilePosition.Random));
        }
    }
}
