using Alchemist.AlchemistCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Antitoxin, CardTheme.Poison)]
public class WarningColors : AlchemistReshuffledCard
{
    public WarningColors() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithNumberedPower<WarningColorsPower>(1, 0);
        WithKeyword(CardKeyword.Innate, UpgradeType.Add);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<WarningColorsPower>(choiceContext, this);
    }
}
