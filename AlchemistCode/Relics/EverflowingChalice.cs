using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Alchemist.AlchemistCode.Relics;

public class EverflowingChalice : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    // Backs the {Energy:energyIcons()} token in the tooltip (base-game relic pattern).
    // "Pending" tracks the fell-back-to-energy state in a DynamicVar (not a plain field)
    // so it survives a mid-combat save/reload; "Energy" backs the tooltip's energy icon.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Energy", 1m), new DynamicVar("Pending", 0m) };

    public override async Task BeforeCombatStart()
    {
        DynamicVars["Pending"].BaseValue = 0m;
        Flash();
        var potion = PotionFactory.CreateRandomPotionOutOfCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        var result = await PotionCmd.TryToProcure(potion, Owner);
        if (!result.success)
            DynamicVars["Pending"].BaseValue = 1m;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (DynamicVars["Pending"].IntValue <= 0) return;
        if (!participants.Contains(Owner.Creature)) return;
        DynamicVars["Pending"].BaseValue = 0m;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
    }
}
