using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Augur : TheAlchemistCard
{
    public Augur() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(8, 4);
        WithPower<WeakPower>(1, 1);
        WithKeyword(CardKeyword.Innate);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<WeakPower>(choiceContext, this, play);
    }
}
