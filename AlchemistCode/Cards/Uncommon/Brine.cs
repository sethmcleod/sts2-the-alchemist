using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Brine : AlchemistCard
{
    public Brine() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithVar("antitoxin", 4, 2);
        WithPower<WeakPower>(2, 0);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, DynamicVars["antitoxin"].IntValue,
            Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(choiceContext, combat.HittableEnemies,
            DynamicVars["WeakPower"].IntValue, Owner.Creature, this);
    }
}
