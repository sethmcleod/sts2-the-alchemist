using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Dross : TheAlchemistCard
{
    public Dross() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(1, 1);
        WithPower<VulnerablePower>(1, 1);
        WithPower<PoisonPower>(1, 1);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<WeakPower>(choiceContext, this, play);
        await CommonActions.Apply<VulnerablePower>(choiceContext, this, play);
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
    }
}
