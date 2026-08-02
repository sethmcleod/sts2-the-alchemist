using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class SlagPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
            HoverTipFactory.Static(StaticHoverTip.Block),
        };

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner != Owner.Player) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
