using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using TheAlchemist.TheAlchemistCode.Commands;

namespace TheAlchemist.TheAlchemistCode.Cards.Common;

public class Carapace : TheAlchemistCard
{
    public Carapace() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(16, 4);
        WithTip(typeof(Toxic));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await AlchemistCardCmd.AddStatus<Toxic>(this);
    }
}
