using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Inducement : AlchemistCard
{
    public Inducement() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("block", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<InducementPower>(choiceContext, Owner.Creature,
            DynamicVars["block"].IntValue, Owner.Creature, this);
    }
}
