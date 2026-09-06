using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Ferment)]
public class TasteTest : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public TasteTest() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("Turns", 2, 1);
        WithCards(1, 0);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && PileType.Hand.GetPile(Owner).Cards.Any(IsBrewing);

    private static LocString Prompt => new("cards", "ALCHEMIST-TASTE_TEST.selectionScreenPrompt");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        var brewing = PileType.Hand.GetPile(Owner).Cards.Where(IsBrewing).ToList();
        if (brewing.Count > 0)
        {
            var chosen = brewing.Count == 1
                ? brewing[0]
                : (await CardSelectCmd.FromHand(choiceContext, Owner,
                    new CardSelectorPrefs(Prompt, 1), IsBrewing, this)).FirstOrDefault();
            if (chosen is AlchemistCard ferment)
            {
                await ferment.AdvanceFerment(DynamicVars["Turns"].IntValue);
                CardCmd.Preview(ferment);
            }
        }
    }
}
