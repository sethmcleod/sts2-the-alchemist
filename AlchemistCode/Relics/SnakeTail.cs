using System.Collections.Generic;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// Antitoxin cures one Poison per point absorbed, so the bar is both the damage you avoid this turn
// and how fast the dose burns down. A bigger ceiling shortens every dose you carry
public class SnakeTail : AlchemistRelic
{
    private const int Capacity = 6;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>(), HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AntitoxinCapacityPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            Capacity, Owner.Creature, null);
    }
}
