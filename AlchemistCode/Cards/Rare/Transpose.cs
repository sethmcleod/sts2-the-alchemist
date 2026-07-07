using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Transpose : AlchemistCard
{
    public Transpose() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithTip(typeof(RegenPower));
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (Owner.Creature.HasPower<RegenPower>())
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
        if (regen <= 0) return;
        var poisonAmount = IsUpgraded ? regen * 2 : regen;
        foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, poisonAmount, Owner.Creature, this);
    }
}
