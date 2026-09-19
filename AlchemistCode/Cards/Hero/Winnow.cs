using System.Linq;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Hero;

[CardTheme(CardTheme.Antitoxin)]
public class Winnow : AlchemistHeroCard
{
    private static LocString Prompt => new("cards", "ALCHEMIST-WINNOW.selectionScreenPrompt");

    public Winnow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Cards", 3, 1);
        WithVar("Antitoxin", 2, 0);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var draw = PileType.Draw.GetPile(Owner);
        if (draw.Cards.Count == 0) return;
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, draw, Owner,
            new CardSelectorPrefs(Prompt, 0, DynamicVars["Cards"].IntValue), static _ => true)).ToList();
        foreach (var card in chosen)
            await CardCmd.Exhaust(choiceContext, card);
        if (chosen.Count == 0) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["Antitoxin"].IntValue * chosen.Count, Owner.Creature, this);
    }
}
