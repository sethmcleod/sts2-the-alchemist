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
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Energy", 1m) };

    private bool _giveEnergy;

    public override async Task BeforeCombatStart()
    {
        _giveEnergy = false;
        Flash();
        var potion = PotionFactory.CreateRandomPotionOutOfCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        var result = await PotionCmd.TryToProcure(potion, Owner);
        if (!result.success)
            _giveEnergy = true;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!_giveEnergy) return;
        if (!participants.Contains(Owner.Creature)) return;
        _giveEnergy = false;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
    }
}
