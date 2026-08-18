using System.Collections.Generic;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// You walk into every fight already dosed and already covered, so the readers are lit from turn 1.
// It used to procure a potion per combat, which is potion income the rubric counts as Meta Scaling
public class EverflowingChalice : AlchemistRelic
{
    private const int Dose = 2;
    private const int Antitoxin = 6;

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
