using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Imbue : AlchemistCard
{
    public Imbue() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Cards", 1, 1);  // random cards Infused each turn: 1 (2)
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ImbuePower>(choiceContext, Owner.Creature,
            DynamicVars["Cards"].IntValue, Owner.Creature, this);
    }
}
