using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Potentiate : TheAlchemistCard
{
    public Potentiate() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var potionsUsed = CombatManager.Instance.History.Entries
            .OfType<PotionUsedEntry>().Count();
        var hitCount = 1 + potionsUsed;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this)
            .Targeting(play.Target!)
            .Execute(choiceContext);
    }
}
