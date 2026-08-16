using System;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Quench : AlchemistCard
{
    private const int PerAntitoxin = 2;
    private const int Grant = 3;
    private const int GrantUpgraded = 4;

    protected internal override bool PlaysCastAnimation => false;

    public Quench() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("antitoxin", Grant, GrantUpgraded - Grant);
        // Counts the grant this card is about to make and clips it to the limit, so the face shows
        // the Block that will land rather than what the bar holds before the play
        WithCalculatedBlock(4, static (card, _) =>
        {
            var creature = card.Owner.Creature;
            var topped = Math.Min(AntitoxinPower.MaxFor(creature),
                creature.GetPowerAmount<AntitoxinPower>() + (card.IsUpgraded ? GrantUpgraded : Grant));
            return topped * PerAntitoxin;
        }, ValueProp.Move, 2, 0);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Antitoxin first, so the Block reads the topped-up bar the card face previewed
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
        await CommonActions.CardBlock(this, play);
    }
}
