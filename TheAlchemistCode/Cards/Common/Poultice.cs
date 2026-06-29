using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Commands;

namespace TheAlchemist.TheAlchemistCode.Cards.Common;

public class Poultice : TheAlchemistCard
{
    public Poultice() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<RegenPower>(2, 1);
        WithTip(typeof(Dazed));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
        await AlchemistCardCmd.AddStatus<Dazed>(this);
    }
}
