using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using Godot;

namespace Alchemist.AlchemistCode.Potions;

// Reads your Gold the way Gold Leaf always did, but thrown now: the fortune becomes the weapon
public class GoldLeaf : AlchemistPotion, IBrewOnly
{
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AllEnemies;

    private static readonly Color GoldTint = new("FFD54A");

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new GoldDamageVar() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var total = (int)(Owner.Gold / 10m);
        if (total <= 0 || Owner.Creature.CombatState is not { } combat) return;
        // Through the shim, one enemy at a time: the multi-target Damage overload is branch-specific
        foreach (var enemy in combat.HittableEnemies.ToList())
        {
            NCombatRoom.Instance?.PlaySplashVfx(enemy, GoldTint);
            await GameCompat.Damage(choiceContext, enemy, total, ValueProp.Unpowered,
                Owner.Creature, null);
        }
    }
}
