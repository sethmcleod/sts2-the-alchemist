using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Accrue : AlchemistCard
{
    public Accrue() : base(2, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    {
        WithDamage(5, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var poisonTicks = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Count(e => e.Receiver == Owner.Creature
                        && e.Dealer == null && e.CardSource == null
                        && e.Result.Props.HasFlag(ValueProp.Unblockable)
                        && e.Result.Props.HasFlag(ValueProp.Unpowered)
                        && e.Result.UnblockedDamage > 0);
        var hitCount = 1 + poisonTicks;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this)
            .Targeting(play.Target!)
            .Execute(choiceContext);
    }
}
