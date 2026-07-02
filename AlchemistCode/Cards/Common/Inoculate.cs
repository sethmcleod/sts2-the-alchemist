using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Inoculate : AlchemistCard
{
    public Inoculate() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 3);
        WithPower<PoisonPower>(3, 0); // self-poison tax: real HP cost for 0-cost block
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
    }
}
