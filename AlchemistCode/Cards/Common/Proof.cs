using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

// Capacity only rises and is never spent, which is what makes it readable as a damage stat.
[CardTheme(CardTheme.Antitoxin)]
public class Proof : AlchemistCard
{
    public Proof() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithVar("antitoxin", 1, 1);
        WithTip(typeof(AntitoxinPower));
    }

    // Zero on the canonical model, so the compendium shows the rule and not a number
    private int Capacity =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<AntitoxinPower>() : 0;

    private int Grant => IsMutable ? DynamicVars["antitoxin"].IntValue : 1;

    // The grant lands before the hit, so this card's own Antitoxin counts toward its damage.
    // Null outside combat: FormulaDamagePreview otherwise falls back to the owner's combat state,
    // which a card on a reward or selection screen still has
    protected override int? RawFormulaDamagePreview =>
        IsMutable && CombatState != null ? Capacity + Grant : null;

    // Powered: an ordinary attack whose number happens to come off a stat, so Strength and
    // Vulnerable apply
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, Grant, Owner.Creature, this);
        if (CombatState == null || play.Target is not { IsAlive: true } target) return;
        var damage = Owner.Creature.GetPowerAmount<AntitoxinPower>();
        if (damage <= 0) return;
        await CommonActions.CardAttack(this, play, target, damage, ValueProp.Move,
                vfx: HitVfx("vfx/vfx_slime_impact"), tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}
