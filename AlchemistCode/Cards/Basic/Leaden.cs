using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Leaden : AlchemistCard
{
    public Leaden() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithUpgradingCardTip<Dross>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await AlchemistCardCmd.GiveCard<Dross>(this);
    }
}
