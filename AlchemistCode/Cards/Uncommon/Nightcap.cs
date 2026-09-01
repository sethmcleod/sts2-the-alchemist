using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Nightcap : AlchemistCard
{
    public Nightcap() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar("Mult", 2, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(AntitoxinPower));
    }

    // Zero on the canonical model, so the compendium shows the rule and not a number
    private int Capacity =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<AntitoxinPower>() : 0;

    protected override bool ConditionalGlow => Capacity > 0;

    // Null outside combat: FormulaDamagePreview otherwise falls back to the owner's combat state,
    // which a card on a reward or selection screen still has
    protected override int? RawFormulaDamagePreview =>
        IsMutable && CombatState != null ? Capacity * DynamicVars["Mult"].IntValue : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target is not { IsAlive: true } target) return;
        var damage = Capacity * DynamicVars["Mult"].IntValue;
        if (damage <= 0) return;
        await CommonActions.CardAttack(this, play, target, damage, ValueProp.Move,
                vfx: HitVfx("vfx/vfx_attack_blunt"), tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}
