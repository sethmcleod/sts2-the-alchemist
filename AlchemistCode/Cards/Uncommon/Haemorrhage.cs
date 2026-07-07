using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Haemorrhage : AlchemistCard
{
    // Damage is computed at play time, so HasFormulaDamage folds in EnchantDamageBonus (Sharp) and
    // renders the green " + N" suffix via {EnchantBonus}.
    protected override bool HasFormulaDamage => true;

    public Haemorrhage() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar("Bonus", 1, 1); // Lose HP equal to Regen + 1 (2)
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        var lost = regen + DynamicVars["Bonus"].IntValue; // Regen + 1 (2) HP
        if (lost > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature,
                lost, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);
        var multiplier = IsUpgraded ? 3 : 2;
        var damage = lost * multiplier + EnchantDamageBonus; // double/triple that much
        if (damage > 0)
            await DamageCmd.Attack(damage).FromCard(this, play)
                .Targeting(play.Target!).Execute(choiceContext);
    }
}
