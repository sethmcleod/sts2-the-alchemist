using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheAlchemist.TheAlchemistCode.Cards.Token;

namespace TheAlchemist.TheAlchemistCode.Cards.Basic;

public class Leaden : TheAlchemistCard
{
    public Leaden() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithUpgradingCardTip<Dross>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (CombatState == null) return;
        CardModel dross = CombatState.CreateCard<Dross>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(dross);
        await CardPileCmd.AddGeneratedCardToCombat(dross, PileType.Hand, Owner);
    }
}
