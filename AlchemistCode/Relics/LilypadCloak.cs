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

public class LilypadCloak : AlchemistRelic
{
    private const int Block = 2;

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner.Creature.CombatState is not { } combat) return;
        if (!combat.GetOpponentsOf(Owner.Creature).Contains(target)) return;
        if (!AntitoxinRules.IsPoisonTick(target, result.UnblockedDamage, props, dealer, cardSource)) return;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, Block, ValueProp.Unpowered, null);
    }
}
