using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Chrysopoeia : AlchemistCard
{
    public Chrysopoeia() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("gold", 3, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ChrysopoeiaPower>(choiceContext, Owner.Creature,
            DynamicVars["gold"].IntValue, Owner.Creature, this);
    }
}
