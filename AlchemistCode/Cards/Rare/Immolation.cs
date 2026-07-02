using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Immolation : AlchemistCard
{
    public Immolation() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Effluvium>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // min 0 / max unbounded = "any number", matching the base game's GUARDS!!! selector.
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, 999999999),
            null, this);
        foreach (var card in selected)
        {
            var effluvium = CombatState!.CreateCard<Effluvium>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(effluvium);
            await CardCmd.Transform(card, effluvium);
        }
    }
}
