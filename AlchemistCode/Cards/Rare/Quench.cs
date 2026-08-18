using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

// The Rare capacity card that also holds the line the turn you play it. Block scaling per
// Antitoxin was the alt-block engine the rubric names; the limit is the axis that is allowed to grow
[CardTheme(CardTheme.Antitoxin)]
public class Quench : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Quench() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Capacity", 3, 1);
        WithBlock(6, 3);
        WithVar("antitoxin", 3, 0);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AntitoxinCapacityPower>(choiceContext, Owner.Creature,
            DynamicVars["Capacity"].IntValue, Owner.Creature, this);
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
