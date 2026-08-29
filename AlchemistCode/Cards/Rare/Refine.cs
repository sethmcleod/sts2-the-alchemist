using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Decant)]
public class Refine : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    private static readonly PileType[] Piles = [PileType.Hand, PileType.Draw, PileType.Discard];

    public Refine() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Decant) });
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        foreach (var pileType in Piles)
        {
            foreach (var card in pileType.GetPile(Owner).Cards)
            {
                if (card is AlchemistCard { IsDecantCard: true } decantCard)
                    decantCard.AddDecant(decantCard.DecantMaxValue);
            }
        }

        return Task.CompletedTask;
    }
}
