using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Decant : AlchemistCard
{
    public Decant() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 2);
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await AlchemistCardCmd.GiveCard<Distillate>(this);
    }
}
