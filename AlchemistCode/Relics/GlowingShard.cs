using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class GlowingShard : AlchemistRelic
{
    private const int Poison = 1;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // Only Poison landing on us. Poison we put on an enemy has a different owner, so this cannot answer
    // its own application and loop
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0m || power is not PoisonPower || power.Owner != Owner.Creature) return;
        if (Owner.Creature.CombatState is not { } combat) return;
        var enemies = combat.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;
        Flash();
        var target = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, Poison, Owner.Creature, null);
    }
}
