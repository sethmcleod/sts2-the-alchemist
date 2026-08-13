using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
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

    // A Poison tick is the only damage that arrives unblockable and unpowered with no dealer and no
    // card. Antitoxin reduces that damage before this runs, so a fully absorbed tick pays nothing here
    // and Warded covers it instead
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature || dealer != null || cardSource != null) return;
        if (!props.HasFlag(ValueProp.Unblockable) || !props.HasFlag(ValueProp.Unpowered)) return;
        if (result.UnblockedDamage <= 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, Block, ValueProp.Unpowered, null);
    }
}
