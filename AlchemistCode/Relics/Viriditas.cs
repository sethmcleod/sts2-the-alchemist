using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class Viriditas : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // Regen decays as it pays out, so the tip saves the player working out what 3 stacks is worth
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<RegenPower>() };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<RegenPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 3m,
            Owner.Creature, null);
    }
}
