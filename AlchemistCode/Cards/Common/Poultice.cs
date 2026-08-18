using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

// The defensive twin of Backfire: Block paid for with a dose, which Antitoxin then absorbs. Also one
// of only three self-Poison sources in the Common band
[CardTheme(CardTheme.Poison)]
public class Poultice : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Poultice() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(9, 3);
        WithVar("poison", 2, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
    }
}
