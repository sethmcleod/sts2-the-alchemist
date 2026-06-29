using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Reagent : TheAlchemistCard
{
    public Reagent() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Unplayable);
        WithVar("block", 4, 2);
    }

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner) return;
        if (Pile?.Type != PileType.Hand) return;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["block"].BaseValue, ValueProp.Move, null);
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, Owner);
    }
}
