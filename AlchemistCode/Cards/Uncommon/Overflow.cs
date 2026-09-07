using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class Overflow : AlchemistCard
{
    public Overflow() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithPower<OverflowPower>(1, 0);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<OverflowPower>(choiceContext, this);
    }
}
