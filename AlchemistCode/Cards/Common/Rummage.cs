using System.Linq;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

// Net-zero card economy, which is the vanilla ceiling for a 0-cost drawer. The value is the choice,
// not the count, so it does not compete with Gulp or Blood Rush
public class Rummage : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    private const int Options = 3;

    public Rummage() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("Options", Options, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var drawPile = PileType.Draw.GetPile(Owner);
        var offered = drawPile.Cards.ToList()
            .StableShuffle(Owner.RunState.Rng.CombatCardSelection)
            .Take(Options)
            .ToList();
        if (offered.Count == 0) return;

        var picked = (await CardSelectCmd.FromCombatPile(choiceContext, drawPile, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), offered.Contains)).FirstOrDefault();
        if (picked == null) return;

        await CardPileCmd.Add(picked, PileType.Hand);
        if (IsUpgraded) picked.SetToFreeThisTurn();
    }
}
