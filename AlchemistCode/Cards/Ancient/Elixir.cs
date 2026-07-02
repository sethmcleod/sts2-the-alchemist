using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Ancient;

public class Elixir : AlchemistCard
{
    public Elixir() : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (IsUpgraded)
            await PotionCmd.TryToProcure(
                PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
                Owner);
        // Explicit amount 1 per copy — stacks procure one potion each at turn start.
        await PowerCmd.Apply<ElixirPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
