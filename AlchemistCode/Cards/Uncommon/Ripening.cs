using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class Ripening : AlchemistCard
{
    public Ripening() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<RipeningPower>(1, 0);
        WithCostUpgradeBy(-1);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<RipeningPower>(choiceContext, this);
    }
}
