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
        // A thrown potion lands with an impact, the way PoisonPotion does. The tint has to be
        // saturated: it is applied as Modulate over greyscale art, so a pale colour washes out
        if (NCombatRoom.Instance?.GetCreatureNode(target) is { } node
            && NGaseousImpactVfx.Create(node.VfxSpawnPosition, new Color("7c4dff")) is { } vfx)
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/workbug_rock/workbug_rock_stun");
        await CreatureCmd.Stun(target!);
    }
}
