using System;
using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// The splash is Unpowered and carries no card, so it never satisfies its own trigger: no recursion,
// and Strength does not count twice
public class OversprayPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || !props.IsPoweredAttack() || result.TotalDamage <= 0) return;
        if (cardSource is not { TargetType: TargetType.AnyEnemy } || !target.HasPower<PoisonPower>()) return;
        if (CombatState == null) return;
        var others = CombatState.Enemies.Where(e => e != target && e.IsAlive && e.IsHittable).ToList();
        var splash = Math.Floor(result.TotalDamage / 2m);
        if (others.Count == 0 || splash <= 0) return;
        Flash();
        for (var i = 0; i < Amount; i++)
            foreach (var enemy in others)
                await GameCompat.Damage(choiceContext, enemy, splash, ValueProp.Unpowered | ValueProp.Move, Owner, null);
    }
}
