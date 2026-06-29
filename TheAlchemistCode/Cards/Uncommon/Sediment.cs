using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Sediment : TheAlchemistCard
{
    public Sediment() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("plating", 1, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(PlatingPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SedimentPower>(choiceContext, Owner.Creature,
            DynamicVars["plating"].IntValue, Owner.Creature, this);
    }
}
