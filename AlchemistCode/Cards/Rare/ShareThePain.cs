using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class ShareThePain : AlchemistCard
{
    public ShareThePain() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // The upgrade buys the cost, the way the Catalyze it replaces did. The damage is the Poison
        // itself, so there is no amount to raise without changing how the card reads
        WithCostUpgradeBy(-1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ShareThePainPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
