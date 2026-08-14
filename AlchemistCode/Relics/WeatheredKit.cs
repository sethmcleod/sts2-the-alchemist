using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.RestSite;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class WeatheredKit : AlchemistRelic
{
    private const int Antitoxin = 3;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.Brew, HoverTipFactory.FromPower<AntitoxinPower>() };

    // Without this, BaseLib falls back to Circlet for the Touch of Orobas starter upgrade
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<GildedKit>();

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            Antitoxin, Owner.Creature, null);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
