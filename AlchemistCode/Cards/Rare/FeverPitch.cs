using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class FeverPitch : AlchemistCard
{
    public FeverPitch() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCards(1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        Owner?.Creature is { } c && c.GetPowerAmount<PoisonPower>() >= FeverPitchPower.Threshold;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<FeverPitchPower>(choiceContext, Owner.Creature,
            DynamicVars.Cards.IntValue, Owner.Creature, this);
    }
}
