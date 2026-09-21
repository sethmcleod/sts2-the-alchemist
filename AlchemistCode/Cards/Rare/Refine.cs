using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Mix)]
public class Refine : AlchemistCard
{
    public Refine() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithQuietPower<RefinePower>(1, 0);
        WithTips(_ => Mixing.MixRefTips());
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<RefinePower>(choiceContext, this);
    }
}
