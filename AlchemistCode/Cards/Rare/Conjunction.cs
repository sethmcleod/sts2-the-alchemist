using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Conjunction : AlchemistCard
{
    public Conjunction() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // Must be WithEnergy, not WithVar — the {Energy:energyIcons()} formatter rejects a plain DynamicVar
        WithEnergy(1, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
    }

    protected override bool ConditionalGlow =>
        Owner?.Creature is { } c && c.HasPower<PoisonPower>() && c.HasPower<RegenPower>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ConjunctionPower>(choiceContext, Owner.Creature,
            DynamicVars["Energy"].IntValue, Owner.Creature, this);
    }
}
