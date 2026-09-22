using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class AllAtOnce : AlchemistCard
{
    protected override bool HasEnergyCostX => true;

    public AllAtOnce() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(0, static (card, _) => Dose(card), ValueProp.Move);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null || Dose(this) <= 0) return;
        var hits = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (hits <= 0) return;
        await CommonActions.CardAttack(this, play, hits, vfx: HitVfx("vfx/vfx_heavy_blunt"),
                tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
