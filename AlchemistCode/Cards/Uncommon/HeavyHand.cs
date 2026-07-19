using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class HeavyHand : AlchemistCard
{
    public HeavyHand() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("extraPoison", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<HeavyHandPower>(choiceContext, Owner.Creature,
            DynamicVars["extraPoison"].IntValue, Owner.Creature, this);
    }
}
