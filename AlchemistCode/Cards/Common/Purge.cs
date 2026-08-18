using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

// The Common reader that pays double: 4 naked, 8 with the starter's dose of 2. Reads the dose and
// leaves it, like every reader
[CardTheme(CardTheme.Poison)]
public class Purge : AlchemistCard
{
    private const int PerPoison = 2;

    public Purge() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(4, static (card, _) => Dose(card) * PerPoison, ValueProp.Move, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact"),
            tmpSfx: "heavy_attack.mp3").Execute(choiceContext);
    }
}
