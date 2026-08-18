using System.Collections.Generic;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Potions;

// Reads your Gold the way Gold Leaf always did, but pays out in Block and Antitoxin now: the heal
// was the one thing on it the rubric names
public class GoldLeaf : AlchemistPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.Static(StaticHoverTip.Block), HoverTipFactory.FromPower<AntitoxinPower>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new GoldHealVar() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var total = (int)(Owner.Gold / 15m);
        if (total <= 0) return;
        await CreatureCmd.GainBlock(Owner.Creature, total, ValueProp.Unpowered, null);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, total, Owner.Creature, null);
    }
}
