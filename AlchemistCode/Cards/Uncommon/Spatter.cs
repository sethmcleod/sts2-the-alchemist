using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Spatter : AlchemistCard
{
    public Spatter() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithVar("Base", 2, 0);
        WithVar("Per", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var discarded = (await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, AnyNumber), null, this)).ToList();
        foreach (var card in discarded)
            await CardCmd.Discard(choiceContext, card);
        var dose = DynamicVars["Base"].IntValue + DynamicVars["Per"].IntValue * discarded.Count;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            PoisonSplash(enemy);
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, dose, Owner.Creature, this);
        }
    }
}
