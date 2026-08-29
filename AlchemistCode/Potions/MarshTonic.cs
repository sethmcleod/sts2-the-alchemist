using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Potions;

public class MarshTonic : AlchemistPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<Powers.AntitoxinPower>(), HoverTipFactory.FromPower<PoisonPower>() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        // The splash and liquid wash every base potion plays, tinted to the Antitoxin colour so the
        // drink reads as the same effect as the bar it fills and the puff an absorb makes
        var creature = target ?? Owner.Creature;
        NCombatRoom.Instance?.PlaySplashVfx(creature, AlchemistModConfig.AntitoxinBarColor);
        await PowerCmd.Apply<Powers.AntitoxinPower>(choiceContext, creature, 4m, Owner.Creature, null);
        await PowerCmd.Apply<PoisonPower>(choiceContext, creature, 2m, Owner.Creature, null);
    }
}
