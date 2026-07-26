using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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
        // Either half is enough, so the pick is never dead: an already upgraded card can still take
        // the Infuses
        bool Selectable(CardModel c) => c.IsUpgradable || Infusion.CanInfuse(c);
        // A lone match resolves with no selection screen, and an infuse alone is a quiet icon, so that
        // case needs a preview unless the upgrade below already gives one
        var autoResolved = Infusion.HandSelectIsAutomatic(Owner, Selectable, 1, 1);
        var picked = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), Selectable, this)).FirstOrDefault();
        if (picked == null) return;
        var upgraded = picked.IsUpgradable;
        if (upgraded)
            CardCmd.Upgrade(picked); // this previews the card on screen
        for (var i = 0; i < DynamicVars["times"].IntValue; i++)
            Infusion.Infuse(picked);
        // Only when the upgrade did not already, so the pick shows without a double popup
        if (autoResolved && !upgraded)
            CardCmd.Preview(picked);
    }
}
