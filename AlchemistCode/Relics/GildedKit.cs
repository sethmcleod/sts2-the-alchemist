using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.RestSite;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class GildedKit : AlchemistRelic
{
    private const int Antitoxin = 10;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.Brew, HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            Antitoxin, Owner.Creature, null);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        if (options.Any(o => o is BrewRestSiteOption)) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
