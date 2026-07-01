using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Epiphany : AlchemistCard
{
    public Epiphany() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCards(2, 1);
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            null, this);
        foreach (var card in selected)
        {
            var distillate = CombatState!.CreateCard<Distillate>(Owner);
            await CardCmd.Transform(card, distillate);
        }
    }
}
