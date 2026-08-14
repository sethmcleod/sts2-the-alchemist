using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Poultice : AlchemistCard
{
    public Poultice() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<AntitoxinPower>(4, 1);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this);

        if (IsUpgraded)
        {
            // The built-in exhaust prompt, because the card's own SelectionScreenPrompt getter throws
            // without a per-card loc key
            var selected = await CardSelectCmd.FromHand(
                choiceContext, Owner,
                new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
                null, this);
            foreach (var card in selected)
                await CardCmd.Exhaust(choiceContext, card);
            return;
        }

        var hand = PileType.Hand.GetPile(Owner);
        if (Owner.RunState.Rng.CombatCardSelection.NextItem(hand.Cards) is { } random)
            await CardCmd.Exhaust(choiceContext, random);
    }
}
