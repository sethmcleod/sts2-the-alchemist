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

    // Upgraded only: the Strength bonus needs 5+ Regen (all of which this loses)
    protected override bool ConditionalGlow =>
        IsUpgraded && Owner?.Creature is { } c && c.GetPowerAmount<RegenPower>() >= 5;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (Owner.Creature.HasPower<RegenPower>())
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
        if (regen > 0)
            await CardPileCmd.Draw(choiceContext, regen, Owner);
        if (IsUpgraded && regen >= 5)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
