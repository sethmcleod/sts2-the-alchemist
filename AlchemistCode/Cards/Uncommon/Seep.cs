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
public class Seep : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Seep() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Bonus", 2, 1);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    private int BrewingCount =>
        IsMutable && Owner != null ? PileType.Hand.GetPile(Owner).Cards.Count(IsBrewing) : 0;

    protected override bool ConditionalGlow => BrewingCount >= 2;

    private static LocString EatPrompt => new("cards", "ALCHEMIST-SEEP.selectionScreenPrompt");
    private static LocString FeedPrompt => new("cards", "ALCHEMIST-SEEP.selectionScreenPromptInto");

    // source is null on both rounds: a second sourced round re-steals the returned card nodes and
    // strands ghost slots in the hand fan (see Transmute)
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (BrewingCount < 2) return;
        var eaten = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(EatPrompt, 1), filter: IsBrewing, source: null!)).FirstOrDefault();
        if (eaten is not AlchemistCard meal) return;
        var fed = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(FeedPrompt, 1), filter: c => c != eaten && IsBrewing(c), source: null!))
            .FirstOrDefault();
        if (fed is not AlchemistCard target) return;
        // Drain and receive move the stored turns without paying Mellow twice; only the bonus is new
        var moved = meal.DrainFerment();
        await CardCmd.Exhaust(choiceContext, eaten);
        target.ReceiveFerment(moved);
        await target.AdvanceFerment(DynamicVars["Bonus"].IntValue);
        CardCmd.Preview(target);
    }
}
