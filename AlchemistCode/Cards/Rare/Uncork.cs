using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Decant)]
public class Uncork : AlchemistCard
{
    public Uncork() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Decant) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<UncorkPower>(choiceContext, Owner.Creature,
            DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
