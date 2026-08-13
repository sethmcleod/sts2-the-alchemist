using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Osmosis : AlchemistCard
{
    public Osmosis() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    private int RawDamage => Owner?.Creature.GetPowerAmount<PoisonPower>() ?? 0;

    protected override int? RawFormulaDamagePreview => RawDamage > 0 ? RawDamage : null;

    protected override bool ConditionalGlow => IsMutable && CombatState != null && RawDamage > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var damage = ApplyEnchantDamage(RawDamage);
        if (damage <= 0) return;
        await DamageCmd.Attack(damage).FromCard(this, play)
            .WithHitFx(HitVfx("vfx/vfx_bloody_impact"))
            .Targeting(play.Target!).Execute(choiceContext);
    }
}
