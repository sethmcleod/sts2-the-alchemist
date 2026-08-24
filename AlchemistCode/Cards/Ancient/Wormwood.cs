using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Ancient;

[CardTheme(CardTheme.Poison)]
public class Wormwood : AlchemistCard
{
    // Matches White Heat: the dose is already the multiplier, so Strength does not compound it
    protected internal override bool DealsUnpoweredDamage => true;

    public Wormwood() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        WithVar("Hits", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    private int Fuel =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<PoisonPower>() : 0;

    private int Hits => IsMutable ? DynamicVars["Hits"].IntValue : 2;

    protected override bool ConditionalGlow => Fuel > 0;

    protected override int? RawFormulaDamagePreview => Fuel > 0 ? Fuel * Hits : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || Fuel <= 0) return;
        await DamageCmd.Attack(Fuel)
            .Unpowered()
            .WithHitCount(Hits)
            .WithHitFx(HitVfx("vfx/vfx_sandy_impact"))
            .FromCard(this, play)
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}
