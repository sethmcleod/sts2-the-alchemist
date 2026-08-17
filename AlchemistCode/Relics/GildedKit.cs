using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.RestSite;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Alchemist.AlchemistCode.Relics;

// The upgraded starter, so it belongs in the event pool with the base characters' upgraded starters
// rather than the character pool, which would make it drop as a ninth relic
[Pool(typeof(EventRelicPool))]
public class GildedKit : AlchemistRelic
{
    private const int Antitoxin = 8;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.Brew, HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            Antitoxin, Owner.Creature, null);
        await AlchemistCardCmd.GiveCardTo<Distillate>(Owner, upgraded: true);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        if (options.Any(o => o is BrewRestSiteOption)) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
