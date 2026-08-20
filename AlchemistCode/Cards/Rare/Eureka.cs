using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Commands;

using MegaCrit.Sts2.Core.CardSelection;

using MegaCrit.Sts2.Core.Commands;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Transform, CardTheme.Mix)]
public class Eureka : AlchemistCard
{
    public Eureka() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCards(2, 1);
        WithVar("transforms", 1, 0);
        WithTips(_ => Mixing.MixTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt,
                DynamicVars["transforms"].IntValue), null, this);
        foreach (var card in selected)
            await Mixing.TransformIntoChosen(choiceContext, Owner, card);
    }
}
