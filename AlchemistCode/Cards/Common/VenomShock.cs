using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class VenomShock : AlchemistCard
{
    public VenomShock() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(10, 3);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => AllEnemiesPoisoned;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = Poisoned(play.Target) ? 2 : 1;
        await CommonActions.CardAttack(this, play, hits, vfx: HitVfx("vfx/vfx_bite")).Execute(choiceContext);
    }
}
