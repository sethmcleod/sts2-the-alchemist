using Alchemist.AlchemistCode.Compat;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class VenomTrance : AlchemistCard
{
    public VenomTrance() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("poison", 8, -2);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => ThresholdMet;

    private bool ThresholdMet =>
        Owner?.Creature?.CombatState is { } combat &&
        combat.GetCreaturesOnSide(CombatSide.Enemy).Any(e =>
            e.IsHittable && e.GetPowerAmount<PoisonPower>() >= DynamicVars["poison"].BaseValue);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!ThresholdMet) return;
        await ExtraTurn.Grant(choiceContext, Owner.Creature, this);
    }
}
