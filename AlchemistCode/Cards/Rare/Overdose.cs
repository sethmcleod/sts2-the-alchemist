using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

// The whole loop in one play: the biggest dose in the pool, and most of the cover for it. The last
// ticks are yours to pay, which is the Rare's risk
[CardTheme(CardTheme.Poison, CardTheme.Antitoxin)]
public class Overdose : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Overdose() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("SelfPoison", 5, 0);
        WithVar("antitoxin", 10, 4);
        WithKeyword(CardKeyword.Exhaust);
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
