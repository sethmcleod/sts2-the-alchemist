using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class BottledFury : AlchemistCard
{
    public BottledFury() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("amount", 1, 1);
        WithTip(typeof(StrengthPower));
        WithTip(typeof(DexterityPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<BottledFuryPower>(choiceContext, Owner.Creature,
            DynamicVars["amount"].IntValue, Owner.Creature, this);
    }
}
