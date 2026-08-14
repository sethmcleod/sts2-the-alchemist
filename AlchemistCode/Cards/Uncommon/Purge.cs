using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Purge : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Purge() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<AntitoxinPower>(3, 2);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this);
    }
}
