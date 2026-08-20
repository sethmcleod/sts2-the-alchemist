using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.RestSite;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Powers;

using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class WeatheredKit : AlchemistRelic
{
    private const int Antitoxin = 8;
    private const int Dose = 1;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.Brew, HoverTipFactory.FromPower<AntitoxinPower>(), HoverTipFactory.FromPower<PoisonPower>() };

    // Without this, BaseLib falls back to Circlet for the Touch of Orobas starter upgrade
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<GildedKit>();

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
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
