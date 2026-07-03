using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Libation : AlchemistCard
{
    public Libation() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("block", 4, 2); // Block per potion held: 4 (6)
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Procure a random potion, then gain Block for each potion you now hold.
        var potion = PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        await PotionCmd.TryToProcure(potion, Owner);
        var count = Owner.Potions.Count();
        if (count > 0)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["block"].BaseValue * count, ValueProp.Move, play);
    }
}
