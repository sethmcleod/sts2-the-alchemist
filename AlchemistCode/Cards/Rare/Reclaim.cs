using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.None)]
public class Reclaim : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Reclaim() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var exhaust = PileType.Exhaust.GetPile(Owner);
        if (exhaust.Cards.Count == 0) return;
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, exhaust, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (chosen == null) return;
        var copy = chosen.CreateClone();
        copy.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
        // A full hand reroutes the add to the Discard Pile; show the player what they chose anyway
        if (copy.Pile?.Type != PileType.Hand) CardCmd.Preview(copy);
    }
}
