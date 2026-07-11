using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Haemorrhage : AlchemistCard
{
    public Haemorrhage() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar("Bonus", 1, 1);
        WithTip(typeof(RegenPower));
    }

    protected override int? FormulaDamagePreview
    {
        get
        {
            if (Owner?.Creature is not { } c) return null;
            var lost = c.GetPowerAmount<RegenPower>() + DynamicVars["Bonus"].IntValue;
            return ApplyEnchantDamage(lost * (IsUpgraded ? 3 : 2));
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        var lost = regen + DynamicVars["Bonus"].IntValue;
        if (lost > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature,
                lost, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);
        var multiplier = IsUpgraded ? 3 : 2;
        var damage = ApplyEnchantDamage(lost * multiplier);
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
