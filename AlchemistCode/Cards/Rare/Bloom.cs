using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Ferment)]
public class Bloom : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Bloom() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Turns", 3, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    private IEnumerable<AlchemistCard> Brewing =>
        !IsMutable || Owner == null
            ? Enumerable.Empty<AlchemistCard>()
            : PileType.Hand.GetPile(Owner).Cards.OfType<AlchemistCard>().Where(c => c.IsFermentInline);

    protected override bool ConditionalGlow => Brewing.Any();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var brewing = Brewing.ToList();
        foreach (var card in brewing)
            card.AdvanceFerment(DynamicVars["Turns"].IntValue);
        if (brewing.Count > 0)
            CardCmd.Preview(brewing.Cast<CardModel>().ToList());
        await Task.CompletedTask;
    }
}
