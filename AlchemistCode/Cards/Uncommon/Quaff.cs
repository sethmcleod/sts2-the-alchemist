using System;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Quaff : AlchemistCard
{
    private const int Grant = 2;
    private const int GrantUpgraded = 3;
    private const int PerAntitoxin = 4;

    protected internal override bool PlaysCastAnimation => false;

    public Quaff() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("antitoxin", Grant, GrantUpgraded - Grant);
        WithCalculatedVar("Cards", 1, static (card, _) =>
        {
            var creature = card.Owner.Creature;
            var topped = Math.Min(AntitoxinPower.MaxFor(creature),
                creature.GetPowerAmount<AntitoxinPower>() + (card.IsUpgraded ? GrantUpgraded : Grant));
            return topped / PerAntitoxin;
        }, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
        var cards = (int)((CalculatedVar)DynamicVars["Cards"]).Calculate(null);
        await CardPileCmd.Draw(choiceContext, cards, Owner);
    }
}
