using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Reflux : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Reflux() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithCostUpgradeBy(-1);
        WithVar("poison", 2, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // The power rides on the chosen ally, the same architecture as the base game's Concoct,
        // so its Owner is the player whose debuff plays it watches
        if (play.Target is not { IsAlive: true } ally || ally == Owner.Creature) return;
        await PowerCmd.Apply<RefluxPower>(choiceContext, ally,
            DynamicVars["poison"].BaseValue, Owner.Creature, this);
    }
}
