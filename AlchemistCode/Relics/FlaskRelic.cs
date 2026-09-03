using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Relics;

// Both flasks open combat the same way and both offer Brew, so the amounts are the only difference.
// Shared so the Poison timing below cannot be fixed on one flask and left wrong on the other
public abstract class FlaskRelic : AlchemistRelic
{
    protected abstract int Antitoxin { get; }
    protected abstract int Dose { get; }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.Brew, HoverTipFactory.FromPower<AntitoxinPower>(), HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, Antitoxin, Owner.Creature, null);
    }

    // PoisonPower triggers and decrements on AfterSideTurnStart, so a dose applied before combat
    // is eaten by the turn-1 trigger before the player can act. Applying it in the Late phase of
    // the first player turn lands after that trigger window; the first tick comes on turn 2
    public override async Task AfterSideTurnStartLate(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (combatState.RoundNumber != 1 || !participants.Contains(Owner.Creature)) return;
        await PowerCmd.Apply<PoisonPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, Dose, Owner.Creature, null);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        if (options.Any(o => o is BrewRestSiteOption)) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
