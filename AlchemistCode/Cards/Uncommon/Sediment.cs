using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Sediment : AlchemistCard
{
    public Sediment() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Block", 2, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(PlatingPower));
    }

    protected override bool ConditionalGlow => IsEnchanted;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SedimentPower>(choiceContext, Owner.Creature,
            DynamicVars["Block"].IntValue, Owner.Creature, this);
        if (IsEnchanted)
            await PowerCmd.Apply<PlatingPower>(choiceContext, Owner.Creature, 3, Owner.Creature, this);
    }
}
