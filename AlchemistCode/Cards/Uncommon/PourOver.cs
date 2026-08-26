using System.Collections.Generic;
using System.Linq;
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
    protected internal override bool PlaysCastAnimation => false;

    public PourOver() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    private IEnumerable<AlchemistCard> Brewing =>
        !IsMutable || Owner == null
            ? Enumerable.Empty<AlchemistCard>()
            : PileType.Hand.GetPile(Owner).Cards.OfType<AlchemistCard>().Where(c => c.IsFermentInline);

    protected override bool ConditionalGlow => Brewing.Count() >= 2;

    private static LocString FromPrompt => new("cards", "ALCHEMIST-POUR_OVER.selectionScreenPrompt");
    private static LocString IntoPrompt => new("cards", "ALCHEMIST-POUR_OVER.selectionScreenPromptInto");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var brewing = Brewing.Cast<CardModel>().ToList();
        if (brewing.Count < 2) return;
        var source = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(FromPrompt, 1),
            filter: c => brewing.Contains(c), source: this)).OfType<AlchemistCard>().FirstOrDefault();
        if (source == null) return;
        var target = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(IntoPrompt, 1),
            filter: c => c != source && brewing.Contains(c), source: this)).OfType<AlchemistCard>().FirstOrDefault();
        if (target == null) return;
        target.ReceiveFerment(source.DrainFerment());
        if (IsUpgraded)
            await target.AdvanceFerment(1);
        CardCmd.Preview(new List<CardModel> { source, target });
    }
}
