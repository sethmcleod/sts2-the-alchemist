using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Inure : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Inure() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("antitoxin", 3, 1);
        WithPower<InurePower>(1, 0);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Antitoxin before the amplifier, so the card's own gain is not amplified. The text lists
        // them in this order and the numbers have to agree with it
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
        await CommonActions.ApplySelf<InurePower>(choiceContext, this);
    }
}
