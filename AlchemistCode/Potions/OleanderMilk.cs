using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Potions;

public class OleanderMilk : AlchemistPotion, IBrewOnly
{
    private const int Triggers = 3;

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        // Bounded on purpose. Draining the whole stack collects N(N+1)/2 at once, so 20 Poison would
        // have paid 210 damage from one Event potion
        for (var i = 0; i < Triggers && target!.IsAlive; i++)
            await PoisonTrigger.Once(choiceContext, target);
    }
}
