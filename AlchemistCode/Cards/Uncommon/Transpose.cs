using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Transpose : AlchemistCard
{
    public Transpose() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("block", 2, 1);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Gain 1 Regen first so the card always converts at least one
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (Owner.Creature.HasPower<RegenPower>())
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
        if (regen <= 0) return;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["block"].BaseValue * regen, ValueProp.Move, play);
    }
}
