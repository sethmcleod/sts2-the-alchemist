using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Decant)]
public class Refine : AlchemistCard
{
    public Refine() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Turns", 1, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Decant) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<RefinePower>(choiceContext, Owner.Creature,
            DynamicVars["Turns"].IntValue, Owner.Creature, this);
    }
}
