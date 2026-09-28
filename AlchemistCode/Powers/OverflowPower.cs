using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// The first fermentation upgrades the card; every later one spills Poison over the field, so a
// card upgraded at a Rest Site pays from its first tick. Poison per turn gained, as Mellow pays
public class OverflowPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef, HoverTipFactory.FromPower<PoisonPower>() };

    internal async Task OnFermented(PlayerChoiceContext choiceContext, AlchemistCard card, int turns)
    {
        if (turns <= 0) return;
        if (card.IsUpgradable)
        {
            Flash();
            CardCmd.Upgrade(card);
            // Upgrade only previews a Deck card on its own; a held card needs the flip to show the change
            CardCmd.Preview(card);
            return;
        }
        if (!card.IsUpgraded || CombatState == null) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, CombatState.HittableEnemies,
            Amount * turns, Owner, null);
    }
}
