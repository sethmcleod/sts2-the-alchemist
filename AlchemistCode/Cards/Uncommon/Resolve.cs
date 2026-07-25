using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Resolve : AlchemistCard
{
    // The gold glow marks the turns the aura is live, the same signal the Gambit attacks use
    protected override bool IsGambitCard => true;

    public Resolve() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Amount", 2, 1);
        WithTip(typeof(StrengthPower));
        WithTip(typeof(DexterityPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ResolvePower>(choiceContext, Owner.Creature,
            DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
