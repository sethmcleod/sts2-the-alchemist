using MegaCrit.Sts2.Core.Entities.Powers;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class RipeningPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { AlchemistTips.FermentRef };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (!PileType.Hand.GetPile(player).Cards.OfType<AlchemistCard>().Any(c => c.IsFermentInline)) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, (int)Amount, player);
    }
}
