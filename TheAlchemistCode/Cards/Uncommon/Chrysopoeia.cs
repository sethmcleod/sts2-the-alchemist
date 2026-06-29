using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Chrysopoeia : TheAlchemistCard
{
    public Chrysopoeia() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("gold", 2, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ChrysopoeiaPower>(choiceContext, Owner.Creature,
            DynamicVars["gold"].IntValue, Owner.Creature, this);
    }
}
