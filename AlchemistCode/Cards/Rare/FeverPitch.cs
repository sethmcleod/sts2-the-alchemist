using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class FeverPitch : AlchemistCard
{
    // IsGambitCard covers both the keyword tooltip and the gold glow, so no bespoke ConditionalGlow
    protected override bool IsGambitCard => true;

    public FeverPitch() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<FeverPitchPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
