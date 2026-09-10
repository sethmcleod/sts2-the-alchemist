using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Alchemist.AlchemistCode.Cards;

// One hit per attack the target intends, on top of the base count. With no target, the only
// hittable enemy stands in, so a one-enemy fight shows the number in hand; with several enemies
// the base count shows until the card is aimed
public sealed class IntentHitsVar : PreviewVar
{
    public IntentHitsVar(string name, int baseValue) : base(name, baseValue)
    {
    }

    protected override int Bonus(AlchemistCard card, Creature? target)
    {
        var enemy = target ?? (card.CombatState is { } combat ? SoleEnemy(combat) : null);
        if (enemy?.Monster == null) return 0;
        var intents = enemy.Monster.NextMove.Intents;
        var hits = 0;
        for (var i = 0; i < intents.Count; i++)
        {
            // An attack intent that does not report its repeats still lands at least once
            if (intents[i] is AttackIntent attack) hits += attack.Repeats > 0 ? attack.Repeats : 1;
        }
        return hits;
    }

    private static Creature? SoleEnemy(ICombatState combat)
    {
        Creature? sole = null;
        var enemies = combat.Enemies;
        for (var i = 0; i < enemies.Count; i++)
        {
            if (!enemies[i].IsHittable) continue;
            if (sole != null) return null;
            sole = enemies[i];
        }
        return sole;
    }
}
