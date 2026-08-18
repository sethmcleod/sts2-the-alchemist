using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Basic;

// The starter's enabler half, and the character in one card: the dose is power, the Antitoxin is
// how you afford it. 3 Antitoxin covers a dose of 2 exactly (ticks of 2 then 1), so the first
// lesson is safe; the second, that a dose bigger than the bar costs HP, arrives with the pool
[CardTheme(CardTheme.Poison)]
public class Dose : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Dose() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithVar("SelfPoison", 2, 0);
        WithVar("antitoxin", 3, 2);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
