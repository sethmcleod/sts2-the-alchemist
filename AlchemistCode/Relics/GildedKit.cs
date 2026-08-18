using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.RestSite;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

using MegaCrit.Sts2.Core.Entities.Creatures;

using MegaCrit.Sts2.Core.Models;

using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// The upgraded starter, so it belongs in the event pool with the base characters' upgraded starters
// rather than the character pool, which would make it drop as a ninth relic
[Pool(typeof(EventRelicPool))]
public class GildedKit : AlchemistRelic
{
    private const int Antitoxin = 6;

    // Reset every combat by BeforeCombatStart, so no state outlives the fight
    private bool _doubled;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.Brew, HoverTipFactory.FromPower<AntitoxinPower>(), HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task BeforeCombatStart()
    {
        _doubled = false;
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            Antitoxin, Owner.Creature, null);
    }

    // The first dose you give yourself each combat is doubled: a bigger opening number for every reader,
    // and the two extra Antitoxin over Weathered Kit are what the bigger dose costs the bar
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (_doubled || canonicalPower is not PoisonPower || amount <= 0) return false;
        if (target != Owner.Creature || applier != Owner.Creature) return false;
        _doubled = true;
        Flash();
        modifiedAmount = amount * 2;
        return true;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        if (options.Any(o => o is BrewRestSiteOption)) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
