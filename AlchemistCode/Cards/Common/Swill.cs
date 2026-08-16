using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Swill : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Swill() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("Turns", 1, 1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    private IEnumerable<AlchemistCard> Brewing =>
        !IsMutable || Owner == null
            ? Enumerable.Empty<AlchemistCard>()
            : PileType.Hand.GetPile(Owner).Cards.OfType<AlchemistCard>().Where(c => c.IsFermentInline);

    protected override bool ConditionalGlow => Brewing.Any();

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var turns = DynamicVars["Turns"].IntValue;
        var brewing = Brewing.ToList();
        foreach (var card in brewing)
            card.AdvanceFerment(turns);
        if (brewing.Count > 0)
            CardCmd.Preview(brewing);
        return Task.CompletedTask;
    }
}
