using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Croak : AlchemistCard
{
    public Croak() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(18, 6);
        WithPower<PoisonPower>(5, 2);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact"),
            tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay).Execute(choiceContext);
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
    }
}
