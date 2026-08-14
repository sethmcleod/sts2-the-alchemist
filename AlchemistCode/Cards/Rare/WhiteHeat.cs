using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class WhiteHeat : AlchemistCard
{
    protected internal override bool DealsUnpoweredDamage => true;

    public WhiteHeat() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
        WithTip(typeof(AntitoxinPower));
    }

    // The pool this spends, so the preview is the damage
    private int Fuel =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<AntitoxinPower>() : 0;

    protected override bool ConditionalGlow => Fuel > 0;
    protected override int? RawFormulaDamagePreview => Fuel > 0 ? Fuel : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var damage = Fuel;
        if (damage <= 0) return;
        await PowerCmd.Remove<AntitoxinPower>(Owner.Creature);
        await DamageCmd.Attack(damage)
            .Unpowered()
            .WithHitFx(HitVfx("vfx/vfx_heavy_blunt"), null, "heavy_attack.mp3")
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}
