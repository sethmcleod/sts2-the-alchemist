using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class EquivalentExchange : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public EquivalentExchange() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Token.BurstingMix>();
        WithUpgradingCardTip<Token.SyrupyMix>();
        WithUpgradingCardTip<Token.ZestyMix>();
    }

    private static int CostOf(CardModel card) =>
        card.EnergyCost.CostsX ? 0 : Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (PileType.Hand.GetPile(Owner).Cards.Count == 0) return;
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), filter: null, source: this))
            .FirstOrDefault();
        if (chosen == null) return;
        var copies = CostOf(chosen);
        await CardCmd.Exhaust(choiceContext, chosen);
        await Mixing.CreateChosenCopies(choiceContext, Owner, copies, IsUpgraded, this);
    }
}
