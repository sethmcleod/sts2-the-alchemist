using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class LashOut : AlchemistCard
{
    private const int Hits = 3;

    public LashOut() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(3, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = Hits + (play.Target?.HasPower<PoisonPower>() == true ? 1 : 0);
        await CommonActions.CardAttack(this, play, hits, vfx: HitVfx("vfx/vfx_attack_slash"))
            .Execute(choiceContext);
    }
}
