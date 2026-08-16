using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

// The Common rung of the cash-out ladder, which previously started at Uncommon with Corrode
public class Purge : AlchemistCard
{
    protected internal override bool DealsUnpoweredDamage => true;

    public Purge() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithVar("Multiplier", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    private int Dose =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<PoisonPower>() : 0;

    private int Multiplier => IsMutable ? DynamicVars["Multiplier"].IntValue : 2;

    protected override bool ConditionalGlow => Dose > 0;

    protected override int? RawFormulaDamagePreview => Dose > 0 ? Dose * Multiplier : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var spent = Dose;
        if (spent <= 0 || play.Target == null) return;

        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -spent, Owner.Creature, this);
        await DamageCmd.Attack(spent * Multiplier)
            .Unpowered()
            .WithHitFx(HitVfx("vfx/vfx_slime_impact"), null, "heavy_attack.mp3")
            .FromCard(this, play)
            .Targeting(play.Target)
            .Execute(choiceContext);
    }
}
