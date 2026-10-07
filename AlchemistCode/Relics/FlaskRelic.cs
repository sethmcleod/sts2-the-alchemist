using Alchemist.AlchemistCode.Commands;
using System.Linq;
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
using Alchemist.AlchemistCode.Potions;
using MegaCrit.Sts2.Core.Rewards;

namespace Alchemist.AlchemistCode.Relics;

public abstract class FlaskRelic : AlchemistRelic
{
    protected abstract int Antitoxin { get; }
    protected virtual int Dose => 0;
    protected virtual bool UpgradedMix => false;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.Brew, HoverTipFactory.FromPower<AntitoxinPower>(), AlchemistTips.MixHeader };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await AntitoxinPower.GrantFrom(this, new ThrowingPlayerChoiceContext(), Owner.Creature, Antitoxin,
            Owner.Creature);
    }

    public override async Task AfterSideTurnStartLate(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (Dose <= 0 || combatState.RoundNumber != 1 || !participants.Contains(Owner.Creature)) return;
        await PowerCmd.Apply<PoisonPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, Dose, Owner.Creature, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;
        await Mixing.CreateRandom(choiceContext, Owner, UpgradedMix, source: this);
    }

    public override Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player == Owner && reward is PotionReward { ClaimedPotion: { } potion } && potion is IBrewOnly)
            Analytics.RunCounters.Tally(Owner, Analytics.RunCounters.BrewPicked + Analytics.RunCounters.Label(potion));
        return Task.CompletedTask;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        if (options.Any(o => o is BrewRestSiteOption)) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
