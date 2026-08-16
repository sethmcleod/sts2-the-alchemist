using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Relics;

public class SecondSkin : AlchemistRelic
{
    private const int Block = 2;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // Reads the pre-absorb tick, the way Callus and Contagion do, so Antitoxin defending you does not
    // also cancel what taking the Poison was supposed to pay
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature) return;
        var tick = result.UnblockedDamage + AntitoxinRules.TickAbsorb(Owner.Creature);
        if (tick <= 0) return;
        if (!AntitoxinRules.IsPoisonTick(Owner.Creature, tick, props, dealer, cardSource)) return;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, Block, ValueProp.Unpowered, null);
    }
}
