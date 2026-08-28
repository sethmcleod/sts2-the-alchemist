using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Transform)]
public class Transmute : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Transmute() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(StaticHoverTip.Transform);
    }

    private static LocString VictimPrompt => new("cards", "ALCHEMIST-TRANSMUTE.selectionScreenPrompt");
    private static LocString ModelPrompt => new("cards", "ALCHEMIST-TRANSMUTE.selectionScreenPromptInto");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (PileType.Hand.GetPile(Owner).Cards.Count < 2) return;
        // source must be null on BOTH rounds: a round with a source subscribes
        // NPlayerHand.OnSelectModeSourceFinished to that source's ExecutionFinished, which fires in
        // the same frame the confirm drained the selected-card row. The queue-freed selected holders
        // are still enumerable that frame, so the late pass re-steals the returned card nodes into
        // fresh holders and strands two empty ghost slots in the hand fan. A null source flushes the
        // parked cards synchronously at confirm and never subscribes
        var victim = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(VictimPrompt, 1), filter: null, source: null!)).FirstOrDefault();
        if (victim == null) return;
        var model = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(ModelPrompt, 1),
            filter: c => c != victim, source: null!)).FirstOrDefault();
        if (model == null) return;
        var copy = model.CreateClone();
        await CardCmd.Transform(victim, copy);
    }
}
