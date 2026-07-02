using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Accrue : AlchemistCard
{
    public Accrue() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithTip(typeof(PoisonPower));
    }

    // Poison ticks taken this combat = self damage with no dealer/card source that is
    // unblockable+unpowered (same detection as ChrysopoeiaPower/InducementPower).
    private int PoisonTicksThisCombat =>
        CombatState == null
            ? 0
            : CombatManager.Instance.History.Entries
                .OfType<DamageReceivedEntry>()
                .Count(e => e.Receiver == Owner.Creature
                            && e.Dealer == null && e.CardSource == null
                            && e.Result.Props.HasFlag(ValueProp.Unblockable)
                            && e.Result.Props.HasFlag(ValueProp.Unpowered)
                            && e.Result.UnblockedDamage > 0);

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // Live hit count in green once extra hits have accrued, e.g. "(Hits 3 times.)".
        var hits = 1 + PoisonTicksThisCombat;
        description.Add("HitsSuffix", hits > 1 ? $"\n(Hits [green]{hits}[/green] times.)" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(1 + PoisonTicksThisCombat)
            .FromCard(this)
            .Targeting(play.Target!)
            .Execute(choiceContext);
    }
}
