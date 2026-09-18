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
    private const int Vulnerable = 2;

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AllEnemies;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<ArtifactPower>(), HoverTipFactory.FromPower<VulnerablePower>() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner.Creature.CombatState is not { } combat) return;
        var enemies = combat.HittableEnemies.ToList();
        foreach (var enemy in enemies)
        {
            if (enemy.Block > 0)
                await GameCompat.LoseBlock(choiceContext, enemy, enemy.Block, Owner.Creature);
            if (enemy.HasPower<ArtifactPower>())
                await PowerCmd.Remove<ArtifactPower>(enemy);
        }
        await PowerCmd.Apply<VulnerablePower>(choiceContext, enemies, Vulnerable, Owner.Creature, null);
    }
}
