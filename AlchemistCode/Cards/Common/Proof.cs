using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Antitoxin)]
public class Proof : AlchemistCard
{
    public Proof() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(0, static (card, _) => card is Proof proof ? proof.HitDamage : 0, ValueProp.Move);
        WithVar("antitoxin", 3, 1);
        WithTip(typeof(AntitoxinPower));
    }

    private int Grant => DynamicVars["antitoxin"].IntValue;

    // The grant lands before the hit, so the preview in hand adds it
    private int HitDamage => AntitoxinCapacity + (Pile?.Type == PileType.Hand ? Grant : 0);

    protected override int? RawFormulaDamagePreview =>
        IsMutable && CombatState != null ? HitDamage : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target is not { IsAlive: true }) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, Grant, Owner.Creature, this);
        if (HitDamage <= 0) return;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_blunt"), tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}
