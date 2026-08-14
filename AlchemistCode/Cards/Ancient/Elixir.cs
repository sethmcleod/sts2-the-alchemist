using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Ancient;

public class Elixir : AlchemistCard
{
    public Elixir() : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (IsUpgraded)
            await Brewing.Produce(Owner, Owner.RunState.Rng.CombatPotionGeneration);
        await PowerCmd.Apply<ElixirPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
