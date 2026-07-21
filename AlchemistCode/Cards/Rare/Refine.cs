using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Refine : AlchemistCard
{
    public Refine() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("times", 2, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // One card takes both effects. The filter accepts a card that either half can improve, so
        // the pick is never dead: an already upgraded card can still take the Infuses
        var picked = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            c => c.IsUpgradable || Infusion.CanInfuse(c), this)).FirstOrDefault();
        if (picked == null) return;
        if (picked.IsUpgradable)
            CardCmd.Upgrade(picked);
        for (var i = 0; i < DynamicVars["times"].IntValue; i++)
            Infusion.Infuse(picked);
    }
}
