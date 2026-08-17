using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class RipenPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) };

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return Task.CompletedTask;
        var brewing = PileType.Hand.GetPile(player).Cards.OfType<AlchemistCard>()
            .Where(c => c.IsFermentInline).ToList();
        if (brewing.Count == 0) return Task.CompletedTask;
        Flash();
        foreach (var card in brewing)
            card.AdvanceFerment((int)Amount);
        CardCmd.Preview(brewing);
        return Task.CompletedTask;
    }
}
