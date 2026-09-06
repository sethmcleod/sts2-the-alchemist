using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Potions;

// The base Gravity power already does the whole job: it counts cards played this turn, deals
// unpowered damage to every hittable enemy, and removes itself at the end of the side's turn.
// Nothing in the base game applies it any more, so a base patch could retire it; the combat tip
// prints the real amount, but the canonical Gravity tip hardcodes 2, so the potion shows no tip
public class VolatileReagent : AlchemistPotion, IBrewOnly
{
    private const int Damage = 5;

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var creature = target ?? Owner.Creature;
        await PowerCmd.Apply<GravityPower>(choiceContext, creature, Damage, Owner.Creature, null);
    }
}
