using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// Tolerance's shape one rung down: the limit is the axis that is allowed to grow
[CardTheme(CardTheme.Antitoxin)]
public class Inure : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Inure() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Capacity", 3, 1);
        WithVar("antitoxin", 3, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AntitoxinCapacityPower>(choiceContext, Owner.Creature,
            DynamicVars["Capacity"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
