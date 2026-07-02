using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Philtre : AlchemistCard
{
    public Philtre() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("amount", 2, 1); // 2 -> 3 upgraded
        WithTip(typeof(StrengthPower));
        WithTip(typeof(DexterityPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PhiltrePower>(choiceContext, Owner.Creature,
            DynamicVars["amount"].IntValue, Owner.Creature, this);
    }
}
