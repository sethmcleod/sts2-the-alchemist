using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Miasma : AlchemistCard
{
    public Miasma() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<PoisonPower>(2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await AlchemistCardCmd.PoisonAll(choiceContext, this);
    }
}
