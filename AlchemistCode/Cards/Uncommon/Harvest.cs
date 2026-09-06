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
public class Harvest : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Harvest() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Turns", 2, 1);
        WithKeyword(CardKeyword.Retain);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    private static LocString Prompt => new("cards", "ALCHEMIST-HARVEST.selectionScreenPrompt");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var draw = PileType.Draw.GetPile(Owner);
        if (!draw.Cards.Any(IsBrewing)) return;
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, draw, Owner,
            new CardSelectorPrefs(Prompt, 1), IsBrewing)).FirstOrDefault();
        if (chosen is not AlchemistCard ferment) return;
        await CardPileCmd.Add(ferment, PileType.Hand);
        await ferment.AdvanceFerment(DynamicVars["Turns"].IntValue);
        CardCmd.Preview(ferment);
    }
}
