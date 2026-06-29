using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Philtre : TheAlchemistCard
{
    public Philtre() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("amount", 1, 1);
        WithTip(typeof(StrengthPower));
        WithTip(typeof(DexterityPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PhiltrePower>(choiceContext, Owner.Creature,
            DynamicVars["amount"].IntValue, Owner.Creature, this);
    }
}
