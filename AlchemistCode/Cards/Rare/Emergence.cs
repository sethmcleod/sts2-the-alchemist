using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Emergence : AlchemistCard
{
    protected override bool IsMettleCard => true;

    public Emergence() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(2, 0);
        WithCards(1, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Mettle) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        var draw = DynamicVars["Cards"].IntValue + (IsReduced ? 1 : 0);
        await CardPileCmd.Draw(choiceContext, draw, Owner);
    }
}
