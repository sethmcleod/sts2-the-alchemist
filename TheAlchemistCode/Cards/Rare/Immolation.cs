using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheAlchemist.TheAlchemistCode.Cards.Token;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Immolation : TheAlchemistCard
{
    public Immolation() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Effluvium>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, Owner.PlayerCombatState!.Hand.Cards.Count()),
            null, this);
        foreach (var card in selected)
        {
            var effluvium = CombatState!.CreateCard<Effluvium>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(effluvium);
            await CardCmd.Transform(card, effluvium);
        }
    }
}
