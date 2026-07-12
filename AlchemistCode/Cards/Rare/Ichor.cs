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

    protected override int? FormulaDamagePreview =>
        Owner?.Creature is { } c && c.MaxHp - c.CurrentHp > 0
            ? ApplyEnchantDamage(c.MaxHp - c.CurrentHp) : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var missingHp = Owner.Creature.MaxHp - Owner.Creature.CurrentHp;
        var damage = ApplyEnchantDamage(missingHp);
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
