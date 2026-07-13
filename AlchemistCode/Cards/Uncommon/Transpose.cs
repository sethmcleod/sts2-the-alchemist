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
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Gain 1 Poison first so the card always converts at least one
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (Owner.Creature.HasPower<PoisonPower>())
            await PowerCmd.Remove<PoisonPower>(Owner.Creature);
        if (poison <= 0) return;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["block"].BaseValue * poison, ValueProp.Move, play);
    }
}
