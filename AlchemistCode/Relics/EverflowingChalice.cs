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

    // "Energy" must be an EnergyVar or the {Energy:energyIcons()} formatter breaks.
    // "Pending" is a DynamicVar (not a field) so it survives a mid-combat save/reload
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new EnergyVar("Energy", 1), new DynamicVar("Pending", 0m) };

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
