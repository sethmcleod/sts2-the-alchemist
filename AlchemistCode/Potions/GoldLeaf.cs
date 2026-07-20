using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Potions;

public class GoldLeaf : AlchemistPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.AnyTime;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new GoldHealVar() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var total = (int)(Owner.Gold / 15m);
        if (total <= 0) return;
        await CreatureCmd.Heal(Owner.Creature, total);
        // The potion is usable at any time, and Block outside a combat has nothing to sit on
        if (Owner.Creature.CombatState != null)
            await CreatureCmd.GainBlock(Owner.Creature, total, ValueProp.Unpowered, null);
    }
}
