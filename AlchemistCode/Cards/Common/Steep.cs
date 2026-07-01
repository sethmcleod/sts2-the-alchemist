using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Steep : AlchemistCard
{
    public Steep() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<PoisonPower>(4, 2);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
    }
}
