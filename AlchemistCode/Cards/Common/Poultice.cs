using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Poultice : AlchemistCard
{
    public Poultice() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<RegenPower>(2, 1);
        WithPower<PoisonPower>(1, 1);
        WithTip(typeof(Dazed));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
        await AlchemistCardCmd.AddStatus<Dazed>(this);
    }
}
