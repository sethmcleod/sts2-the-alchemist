using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class PassItOn : AlchemistCard
{
    public PassItOn() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PassItOnPower>(choiceContext, Owner.Creature, DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
