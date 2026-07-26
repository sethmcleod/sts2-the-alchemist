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

    // Shared by the preview and the real hit, so the two cannot drift apart
    private int Damage() => ApplyEnchantDamage(RawDamage());

    private int RawDamage() => Owner.Creature.MaxHp - Owner.Creature.CurrentHp;

    // Raw, because AlchemistCard runs the enchantment and global damage hooks on it
    protected override int? RawFormulaDamagePreview
    {
        get
        {
            if (Owner?.Creature is null) return null;
            var damage = RawDamage();
            return damage > 0 ? damage : null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var damage = Damage();
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .WithHitFx(HitVfx("vfx/vfx_bloody_impact"), null, "heavy_attack.mp3")
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
