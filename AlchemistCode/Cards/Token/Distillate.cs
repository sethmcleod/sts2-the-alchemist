using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Distillate : AlchemistCard
{
    public Distillate() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithBlock(3, 2);
        WithPower<RegenPower>(1, 1);
        WithCards(1, 0);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
        await CommonActions.Draw(this, choiceContext);
    }
}
