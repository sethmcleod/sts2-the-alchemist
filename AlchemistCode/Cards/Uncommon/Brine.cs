using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison, CardTheme.Antitoxin)]
public class Brine : AlchemistCard
{
    public Brine() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithVar("antitoxin", 2, 1);
        WithPower<WeakPower>(2, 1);
        WithTip(typeof(AntitoxinPower));
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && CombatState != null && CombatState.HittableEnemies.Any(Poisoned);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, DynamicVars["antitoxin"].IntValue,
            Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(choiceContext, combat.HittableEnemies.Where(Poisoned),
            DynamicVars["WeakPower"].IntValue, Owner.Creature, this);
    }
}
