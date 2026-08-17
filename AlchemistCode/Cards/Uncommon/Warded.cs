using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Warded : AlchemistCard
{
    public Warded() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Block", 1, 1);
        WithTip(StaticHoverTip.Block);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<WardedPower>(choiceContext, Owner.Creature,
            DynamicVars["Block"].IntValue, Owner.Creature, this);
    }
}
