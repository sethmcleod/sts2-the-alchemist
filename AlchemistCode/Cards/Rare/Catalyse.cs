using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Catalyse : AlchemistCard
{
    public Catalyse() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Cards", 2, 0); // random cards Infused on the first HP change each turn (fixed 2)
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<CatalysePower>(choiceContext, Owner.Creature,
            DynamicVars["Cards"].IntValue, Owner.Creature, this);
    }
}
