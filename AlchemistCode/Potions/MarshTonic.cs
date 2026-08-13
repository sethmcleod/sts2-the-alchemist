using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Potions;

public class MarshTonic : AlchemistPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[]
        {
            HoverTipFactory.FromPower<Powers.AntitoxinPower>(),
            HoverTipFactory.FromPower<PoisonPower>(),
        };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 3m, Owner.Creature, null);
        await PowerCmd.Apply<Powers.AntitoxinPower>(choiceContext, Owner.Creature, 5m, Owner.Creature, null);
    }
}
