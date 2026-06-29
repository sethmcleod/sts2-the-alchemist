using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheAlchemist.TheAlchemistCode.Cards.Token;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Salvage : TheAlchemistCard
{
    public Salvage() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var selected = await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Discard.GetPile(Owner), Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2));
        foreach (var card in selected)
        {
            var distillate = CombatState.CreateCard<Distillate>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(distillate);
            await CardCmd.Transform(card, distillate);
        }
    }
}
