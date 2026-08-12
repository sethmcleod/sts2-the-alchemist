using Alchemist.AlchemistCode.Compat;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Hemorrhage : AlchemistCard
{
    public Hemorrhage() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar("mult", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    // Shared by the on-card preview and the real hit, so the two cannot drift apart
    private int RawDamageFor(int poison) => poison * DynamicVars["mult"].IntValue;
    private int DamageFor(int poison) => ApplyEnchantDamage(RawDamageFor(poison));

    // Raw, because AlchemistCard runs the enchantment and global damage hooks on it
    protected override int? RawFormulaDamagePreview
    {
        get
        {
            if (Owner?.Creature is not { } c) return null;
            var poison = c.GetPowerAmount<PoisonPower>();
            return poison > 0 ? RawDamageFor(poison) : null;
        }
    }

    protected override int? FormulaHpLossPreview
    {
        get
        {
            if (Owner?.Creature is not { } c) return null;
            var poison = c.GetPowerAmount<PoisonPower>();
            return poison > 0 ? poison : null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var lost = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (lost > 0)
            await GameCompat.Damage(choiceContext, Owner.Creature,
                lost, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);

        var damage = DamageFor(lost);
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .WithHitFx(HitVfx("vfx/vfx_bloody_impact"))
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
