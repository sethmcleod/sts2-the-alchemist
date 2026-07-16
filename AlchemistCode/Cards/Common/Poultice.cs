using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Poultice : AlchemistCard
{
    public Poultice() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<RegenPower>(2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Use the built-in exhaust prompt, since the card's own SelectionScreenPrompt getter throws without a per-card loc key
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            null, this);
        foreach (var card in selected)
            await CardCmd.Exhaust(choiceContext, card);

        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
