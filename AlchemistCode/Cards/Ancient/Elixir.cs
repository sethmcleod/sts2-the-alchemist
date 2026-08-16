using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Ancient;

public class Elixir : AlchemistCard
{
    public Elixir() : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
        WithVar("Multiplier", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ElixirPower>(choiceContext, Owner.Creature,
            DynamicVars["Multiplier"].IntValue, Owner.Creature, this);
    }
}
