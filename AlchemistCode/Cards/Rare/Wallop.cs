using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.None)]
public class Wallop : AlchemistCard
{
    public Wallop() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithCalculatedVar("CalculatedHits", 0, static (card, _) => OtherHandCount(card));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = OtherHandCount(this);
        if (hits <= 0) return;
        await CommonActions.CardAttack(this, play, hits,
                vfx: HitVfx("vfx/vfx_heavy_blunt"), tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
