using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Antitoxin, CardTheme.Poison)]
public class Tolerance : AlchemistCard
{
    public Tolerance() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<TolerancePower>(1, 0);
        WithKeyword(CardKeyword.Ethereal, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<TolerancePower>(choiceContext, this);
    }
}
