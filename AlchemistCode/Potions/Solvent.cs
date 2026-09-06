using System.Linq;
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

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AllEnemies;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<ArtifactPower>() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner.Creature.CombatState is not { } combat) return;
        foreach (var enemy in combat.HittableEnemies.ToList())
        {
            if (enemy.Block > 0)
                await GameCompat.LoseBlock(choiceContext, enemy, enemy.Block, Owner.Creature);
            if (enemy.HasPower<ArtifactPower>())
                await PowerCmd.Remove<ArtifactPower>(enemy);
        }
    }
}
