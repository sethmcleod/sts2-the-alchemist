using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The control lever for Ferment: pick the card that ripens, rather than waiting for it
[CardTheme(CardTheme.Ferment)]
public class Steep : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Steep() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Turns", 2, 1);
        WithCards(1, 0);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    private IEnumerable<AlchemistCard> Brewing =>
        !IsMutable || Owner == null
            ? Enumerable.Empty<AlchemistCard>()
            : PileType.Hand.GetPile(Owner).Cards.OfType<AlchemistCard>().Where(c => c.IsFermentInline && c != this);

    protected override bool ConditionalGlow => Brewing.Any();

    private static LocString SelectPrompt => new("card_keywords", "ALCHEMIST-FERMENT.selectionPrompt");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var options = Brewing.Cast<CardModel>().ToList();
        if (options.Count > 0)
        {
            var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner,
                new CardSelectorPrefs(SelectPrompt, 1),
                filter: c => options.Contains(c), source: this)).OfType<AlchemistCard>().FirstOrDefault();
            if (chosen != null)
            {
                chosen.AdvanceFerment(DynamicVars["Turns"].IntValue);
                CardCmd.Preview(new List<CardModel> { chosen });
            }
        }
        await CommonActions.Draw(this, choiceContext);
    }
}
