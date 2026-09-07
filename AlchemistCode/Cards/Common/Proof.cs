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
        WithVar("antitoxin", 2, 1);
        WithTip(typeof(AntitoxinPower));
    }

    private int Grant => DynamicVars["antitoxin"].IntValue;

    // The grant lands before the hit, so this card's own Antitoxin counts toward its damage
    protected override int? RawFormulaDamagePreview =>
        IsMutable && CombatState != null ? AntitoxinCapacity + Grant : null;

    // Powered: an ordinary attack whose number happens to come off a stat, so Strength and
    // Vulnerable apply
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, Grant, Owner.Creature, this);
        var damage = AntitoxinCapacity;
        if (damage <= 0) return;
        await CommonActions.CardAttack(this, play, target, damage, ValueProp.Move,
                vfx: HitVfx("vfx/vfx_slime_impact"), tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}
