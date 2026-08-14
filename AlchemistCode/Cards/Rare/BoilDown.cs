using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class BoilDown : AlchemistCard
{
    public BoilDown() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Distillate>();
        WithTip(StaticHoverTip.Transform);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // The shared TransformSelectionPrompt prints the max count, which is the AnyNumber sentinel here,
        // so this takes a per-card prompt with no count, as the base game Guards does
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, AnyNumber),
            null, this);
        foreach (var card in selected)
        {
            var distillate = CombatState!.CreateCard<Distillate>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(distillate);
            await CardCmd.Transform(card, distillate);
        }
    }
}
