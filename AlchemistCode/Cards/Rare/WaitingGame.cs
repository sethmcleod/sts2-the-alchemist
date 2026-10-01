using Alchemist.AlchemistCode.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class WaitingGame : AlchemistCard
{
    public WaitingGame() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithNumberedPower<WaitingGamePower>(1, 0);
        WithKeyword(CardKeyword.Ethereal, UpgradeType.Remove);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<WaitingGamePower>(choiceContext, Owner.Creature,
            DynamicVars["WaitingGamePower"].IntValue, Owner.Creature, this);
    }
}
