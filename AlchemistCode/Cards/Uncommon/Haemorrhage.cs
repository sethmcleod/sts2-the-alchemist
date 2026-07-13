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
        WithTip(typeof(RegenPower));
    }

    protected override int? FormulaDamagePreview
    {
        get
        {
            if (Owner?.Creature is not { } c) return null;
            var regen = c.GetPowerAmount<RegenPower>();
            if (regen <= 0) return null; // no Regen — nothing lost, nothing dealt
            return ApplyEnchantDamage(regen * (IsUpgraded ? 3 : 2));
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var lost = Owner.Creature.GetPowerAmount<RegenPower>();
        if (lost > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature,
                lost, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);
        var damage = ApplyEnchantDamage(lost * (IsUpgraded ? 3 : 2));
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
