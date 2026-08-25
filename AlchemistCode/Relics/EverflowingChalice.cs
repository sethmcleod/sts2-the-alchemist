using System.Collections.Generic;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// The Mix lane's relic: the shelf is stocked before every fight, your pick
public class EverflowingChalice : AlchemistRelic
{
    private const int Dose = 2;
    private const int Antitoxin = 2;

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task BeforeCombatStart()
    {
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<PoisonPower>(ctx, Owner.Creature, Dose, Owner.Creature, null);
        await PowerCmd.Apply<AntitoxinPower>(ctx, Owner.Creature, Antitoxin, Owner.Creature, null);
    }
}
