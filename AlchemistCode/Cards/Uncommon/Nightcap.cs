using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
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
        WithCalculatedDamage(0, static (card, _) => card is Nightcap nightcap ? nightcap.HitDamage : 0, ValueProp.Move);
        WithVar("antitoxin", 2, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(AntitoxinPower));
    }

    private int Grant => DynamicVars["antitoxin"].IntValue;

    private int Mult => IsUpgraded ? 3 : 2;

    // The grant lands before the hit, so the preview in hand adds it
    private int HitDamage => (AntitoxinCapacity + (Pile?.Type == PileType.Hand ? AntitoxinGrant(Grant) : 0)) * Mult;

    protected override int? RawFormulaDamagePreview =>
        IsMutable && CombatState != null ? HitDamage : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target is not { IsAlive: true }) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, Grant, Owner.Creature, this);
        if (HitDamage <= 0) return;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"), tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
