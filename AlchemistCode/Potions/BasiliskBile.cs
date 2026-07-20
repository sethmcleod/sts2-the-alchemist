using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Potions;

public class BasiliskBile : AlchemistPotion, IBrewOnly
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AllEnemies;

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var combatState = Owner.Creature.CombatState!;
        for (var i = 0; i < 2; i++)
        {
            foreach (var enemy in combatState.GetCreaturesOnSide(CombatSide.Enemy).Where(e => e.IsHittable).ToList())
            {
                var poison = enemy.GetPowerAmount<PoisonPower>();
                if (poison <= 0) continue;
                await CreatureCmd.Damage(choiceContext, enemy, poison,
                    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner.Creature, null, null);
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, -1, Owner.Creature, null);
            }
        }
    }
}
