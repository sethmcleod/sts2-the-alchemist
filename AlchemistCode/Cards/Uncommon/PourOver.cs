using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class PourOver : AlchemistCard
{
    protected override bool Ferments => true;

    protected internal override bool PlaysCastAnimation => false;

    public PourOver() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 0);
        WithVar("Bonus", 0, 1);
        WithKeyword(CardKeyword.Retain);
    }

    private IEnumerable<AlchemistCard> Brewing =>
        !IsMutable || Owner == null
            ? Enumerable.Empty<AlchemistCard>()
            : PileType.Hand.GetPile(Owner).Cards.OfType<AlchemistCard>()
                .Where(c => c != this && c.IsFermentInline);

    private int Bonus => DynamicVars["Bonus"].IntValue;

    private bool CanPour => (HasStoredFerment || Bonus > 0) && Brewing.Any();

    protected override bool ConditionalGlow => CanPour;

    private static LocString IntoPrompt => new("cards", "ALCHEMIST-POUR_OVER.selectionScreenPromptInto");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        if (!CanPour) return;
        var target = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(IntoPrompt, 1),
            filter: c => c is AlchemistCard { IsFermentInline: true } && c != this,
            source: null!)).OfType<AlchemistCard>().FirstOrDefault();
        if (target == null) return;
        target.ReceiveFerment(DrainFerment());
        if (Bonus > 0) await target.AdvanceFerment(Bonus);
        CardCmd.Preview(new List<CardModel> { target });
    }
}
