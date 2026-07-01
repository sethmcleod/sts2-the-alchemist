using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Falter : AlchemistCard
{
    public Falter() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(8, 2);
        WithPower<WeakPower>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.ApplySelf<WeakPower>(choiceContext, this);
    }
}
