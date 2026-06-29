using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Compound : TheAlchemistCard
{
    private bool AppliedPoisonThisTurn =>
        CombatState != null && CombatManager.Instance.History.Entries
            .OfType<PowerReceivedEntry>()
            .Any(e => e.Power is PoisonPower && e.HappenedThisTurn(CombatState));

    protected override bool ShouldGlowGoldInternal => AppliedPoisonThisTurn;

    public Compound() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hitCount = AppliedPoisonThisTurn ? 4 : 2;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this)
            .Targeting(play.Target!)
            .Execute(choiceContext);
    }
}
