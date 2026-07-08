using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Flush : AlchemistCard
{
    public Flush() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithTip(typeof(RegenPower));
        WithTip(typeof(StrengthPower));
    }

    // Upgraded only: the Strength bonus needs to lose 3+ Regen, i.e. hold 5+ (it keeps 2)
    protected override bool ConditionalGlow =>
        IsUpgraded && Owner?.Creature is { } c && c.GetPowerAmount<RegenPower>() >= 5;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var lost = Math.Max(0, Owner.Creature.GetPowerAmount<RegenPower>() - 2);
        if (lost > 0)
        {
            if (Owner.Creature.GetPower<RegenPower>() is { } regenPower)
                await PowerCmd.ModifyAmount(choiceContext, regenPower, -lost, Owner.Creature, this);
            await CardPileCmd.Draw(choiceContext, lost, Owner);
        }
        if (IsUpgraded && lost >= 3)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
