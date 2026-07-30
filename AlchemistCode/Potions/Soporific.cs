using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Alchemist.AlchemistCode.Potions;

public class Soporific : AlchemistPotion, IBrewOnly
{
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Stun)];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        // A thrown potion lands with an impact, the way PoisonPotion does
        if (NCombatRoom.Instance?.GetCreatureNode(target) is { } node)
        {
            var vfx = NGaseousImpactVfx.Create(node.VfxSpawnPosition, new Color("b39ddb"));
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        }
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/workbug_silk/workbug_silk_stun");
        await CreatureCmd.Stun(target!);
    }
}
