using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Hero;

// Pommel Strike is the base band. The Poison gate on the draw pays for the extra damage. The gate
// is read before the hit, so a kill still draws
[CardTheme(CardTheme.Poison)]
public class SnackAttack : AlchemistHeroCard
{
    public SnackAttack() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(10, 2);
        WithCards(1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => AllEnemiesPoisoned;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var poisoned = Poisoned(play.Target);
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        if (poisoned)
            await CommonActions.Draw(this, choiceContext);
    }
}
