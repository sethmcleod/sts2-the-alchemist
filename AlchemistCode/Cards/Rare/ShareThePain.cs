using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class ShareThePain : AlchemistCard
{
    public ShareThePain() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ShareThePainPower>(choiceContext, Owner.Creature, DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
