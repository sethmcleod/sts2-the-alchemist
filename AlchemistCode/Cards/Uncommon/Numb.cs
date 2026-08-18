using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// Poison yourself for a big wall now. Tempered's shape at Uncommon: bigger than a 2-cost Block card
// has any right to be, and the dose is the bill
[CardTheme(CardTheme.Poison)]
public class Numb : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Numb() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(14, 4);
        WithVar("SelfPoison", 3, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
    }
}
