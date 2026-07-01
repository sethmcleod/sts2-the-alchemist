using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Suffuse : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Suffuse() : base(2, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
    {
        WithVar("regen", 3, 1);
        WithTip(typeof(RegenPower));
        WithTip(typeof(WeakPower));
        WithTip(typeof(VulnerablePower));
        WithTip(typeof(FrailPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var allies = from c in CombatState!.GetTeammatesOf(Owner.Creature)
            where c != null && c.IsAlive && c.IsPlayer
            select c;
        foreach (var ally in allies)
        {
            await PowerCmd.Apply<RegenPower>(choiceContext, ally, DynamicVars["regen"].BaseValue, Owner.Creature, this);
            if (ally.HasPower<WeakPower>()) await PowerCmd.Remove<WeakPower>(ally);
            if (ally.HasPower<VulnerablePower>()) await PowerCmd.Remove<VulnerablePower>(ally);
            if (ally.HasPower<FrailPower>()) await PowerCmd.Remove<FrailPower>(ally);
        }
    }
}
