using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.None)]
public class Tempered : AlchemistCard
{
    private const int PerCard = 4;

    public Tempered() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCalculatedBlock(8, static (card, _) =>
                PerCard * PileType.Exhaust.GetPile(card.Owner).Cards.Count,
            ValueProp.Move, 4, 0);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
