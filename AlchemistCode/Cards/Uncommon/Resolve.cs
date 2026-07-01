using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Resolve : AlchemistCard
{
    public Resolve() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("hpThreshold", 15, -5);
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ResolvePower>(choiceContext, Owner.Creature,
            DynamicVars["hpThreshold"].IntValue, Owner.Creature, this);
    }
}
