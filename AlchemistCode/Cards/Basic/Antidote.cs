using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Antidote : AlchemistCard
{
    public Antidote() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(4, 3);
        WithPower<AntitoxinPower>(2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this);
    }
}
