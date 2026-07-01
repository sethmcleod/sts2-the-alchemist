using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Reconstitute : AlchemistCard
{
    public Reconstitute() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<PoisonPower>(2, 0);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
        var selected = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Discard.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Hand);
    }
}
