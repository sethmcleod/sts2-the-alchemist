using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Relics;

public class Quintessence : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    // The relic names Reaction without carrying a condition of its own, so it shows the condition-free tip
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { AlchemistTips.Reaction };

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature)) return;
        Flash();
        await PowerCmd.Apply<ReactivePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1,
            Owner.Creature, null);
    }
}
