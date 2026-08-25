using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Potions;

public class Solvent : AlchemistPotion, IBrewOnly
{
    private const int Weak = 3;

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<ArtifactPower>(), HoverTipFactory.FromPower<WeakPower>() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        if (target!.Block > 0)
            await GameCompat.LoseBlock(choiceContext, target, target.Block, Owner.Creature);
        if (target.HasPower<ArtifactPower>())
            await PowerCmd.Remove<ArtifactPower>(target);
        await PowerCmd.Apply<WeakPower>(choiceContext, target, Weak, Owner.Creature, null);
    }
}
