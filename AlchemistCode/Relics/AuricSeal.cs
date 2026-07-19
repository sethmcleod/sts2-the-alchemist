using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Relics;

public class AuricSeal : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => Infusion.InfuseTips();

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature)) return Task.CompletedTask;
        if (!PileType.Hand.GetPile(Owner).Cards.Any(Infusion.CanInfuse)) return Task.CompletedTask;
        Flash();
        Infusion.InfuseRandomFromHand(Owner, 1);
        return Task.CompletedTask;
    }
}
