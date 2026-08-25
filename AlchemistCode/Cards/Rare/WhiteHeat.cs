using Alchemist.AlchemistCode.Compat;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class WhiteHeat : AlchemistCard
{
    protected internal override bool DealsUnpoweredDamage => true;

    public WhiteHeat() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithVar("Multiplier", 3, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    private int Fuel =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<PoisonPower>() : 0;

    private int Multiplier => IsMutable ? DynamicVars["Multiplier"].IntValue : 3;

    protected override bool ConditionalGlow => Fuel > 0;

    protected override int? RawFormulaDamagePreview => Fuel > 0 ? Fuel * Multiplier : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || Fuel <= 0) return;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(Fuel * Multiplier)
            .Unpowered()
            .WithHitFx(HitVfx("vfx/vfx_fire_burst"), "event:/sfx/characters/attack_fire")
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}
