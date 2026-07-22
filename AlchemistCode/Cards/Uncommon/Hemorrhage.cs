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
        WithCostUpgradeBy(-1);
        WithTip(typeof(RegenPower));
    }

    // Single source of truth for the on-card preview and the real hit. You lose your Regen as HP, then deal
    // double that much, after enchant multipliers
    private int RawDamageFor(int regen) => regen * 2;

    private int DamageFor(int regen) => ApplyEnchantDamage(RawDamageFor(regen));

    // The raw total. AlchemistCard runs the enchantment hooks and the global damage hooks on it
    protected override int? RawFormulaDamagePreview
    {
        get
        {
            if (Owner?.Creature is not { } c) return null;
            var regen = c.GetPowerAmount<RegenPower>();
            return regen > 0 ? RawDamageFor(regen) : null;
        }
    }

    protected override int? FormulaHpLossPreview
    {
        get
        {
            if (Owner?.Creature is not { } c) return null;
            var regen = c.GetPowerAmount<RegenPower>();
            return regen > 0 ? regen : null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var lost = Owner.Creature.GetPowerAmount<RegenPower>();
        if (lost > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature,
                lost, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);
        var damage = DamageFor(lost);
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
