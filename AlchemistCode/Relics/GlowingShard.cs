using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// The base Poison trigger counts Accelerant on the poisoned creature's opponents, so one stack on
// the player doubles every enemy tick and never touches the player's own Poison
public class GlowingShard : AlchemistRelic
{
    private const int Stacks = 1;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AccelerantPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, Stacks, Owner.Creature, null);
    }
}
