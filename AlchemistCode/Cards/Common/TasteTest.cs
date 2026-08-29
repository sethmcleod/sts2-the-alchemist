using Alchemist.AlchemistCode;
using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Ferment)]
public class TasteTest : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public TasteTest() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 1);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
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
            await card.AdvanceFerment(1);
        if (brewing.Count == 0) return;
        CardCmd.Preview(brewing);
        await CommonActions.Draw(this, choiceContext);
    }
}
