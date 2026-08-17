using Alchemist.AlchemistCode.Compat;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class AllAtOnce : AlchemistCard
{
    protected internal override bool DealsUnpoweredDamage => true;

    protected override bool HasEnergyCostX => true;

    public AllAtOnce() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    // The pool this spends, so the preview is the damage per hit
    private int Fuel =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<PoisonPower>() : 0;

    protected override bool ConditionalGlow => Fuel > 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Fuel", Fuel is var f and > 0 ? $" ([green]{f}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var damage = Fuel;
        if (damage <= 0) return;
        await PowerCmd.Remove<PoisonPower>(Owner.Creature);
        var hits = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (hits <= 0) return;
        await DamageCmd.Attack(damage)
            .WithHitCount(hits)
            .Unpowered()
            .WithHitFx(HitVfx("vfx/vfx_heavy_blunt"), null, "heavy_attack.mp3")
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}
