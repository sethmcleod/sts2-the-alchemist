using Alchemist.AlchemistCode.Compat;
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
        WithVar("Multiplier", 2, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(AntitoxinPower));
    }

    // The pool this spends, so the preview is the damage
    private int Fuel =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<AntitoxinPower>() : 0;

    protected override bool ConditionalGlow => Fuel > 0;
    private int Multiplier => IsMutable ? DynamicVars["Multiplier"].IntValue : 2;

    protected override int? RawFormulaDamagePreview => Fuel > 0 ? Fuel * Multiplier : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var spent = Fuel;
        if (spent <= 0) return;
        var damage = spent * Multiplier;
        // Routed through the absorb chain rather than a bare Remove, so Warded, Pass It On and
        // Pays Off all see the spend the way they do for a Poison tick
        await PowerCmd.Remove<AntitoxinPower>(Owner.Creature);
        AntitoxinRules.MarkAbsorbed(Owner.Creature, spent);
        if (Owner.Creature.GetPower<PassItOnPower>() is { } passItOn)
            await passItOn.OnAbsorbed(spent);
        if (Owner.Creature.GetPower<WardedPower>() is { } warded)
            await warded.OnAbsorbed(spent);
        await DamageCmd.Attack(damage)
            .Unpowered()
            .WithHitFx(HitVfx("vfx/vfx_heavy_blunt"), null, "heavy_attack.mp3")
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}
