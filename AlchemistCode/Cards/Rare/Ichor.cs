using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Ichor : AlchemistCard
{
    public Ichor() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithCostUpgradeBy(-1);
    }

    // Deals damage equal to your missing HP (after enchant multipliers) — shared by preview and the real hit
    private int Damage() => ApplyEnchantDamage(Owner.Creature.MaxHp - Owner.Creature.CurrentHp);

    protected override int? FormulaDamagePreview
    {
        get
        {
            if (Owner?.Creature is null) return null;
            var damage = Damage();
            return damage > 0 ? damage : null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var damage = Damage();
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
