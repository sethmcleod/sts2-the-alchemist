using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix, CardTheme.Poison)]
public class Runoff : AlchemistCard
{
    public Runoff() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<RunoffPower>(choiceContext, Owner.Creature,
            DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
