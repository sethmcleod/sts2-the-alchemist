using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Inure : AlchemistCard
{
    public Inure() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<InurePower>(1, 0);
        WithVar("antitoxin", 3, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // The amplifier lands before the gain, so the card's own Antitoxin IS amplified. The text
        // lists them in this order and the numbers have to agree with it
        await CommonActions.ApplySelf<InurePower>(choiceContext, this);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
