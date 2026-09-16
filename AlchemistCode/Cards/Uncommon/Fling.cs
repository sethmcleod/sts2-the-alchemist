using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Fling : AlchemistCard
{
    public Fling() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, 0);
        WithVar("Per", 3, -1);
        WithCalculatedVar("CalculatedHits", 0, static (card, _) => Hits(card));
        WithTip(typeof(PoisonPower));
    }

    private static int Hits(CardModel card) => 1 + (int)Dose(card) / card.DynamicVars["Per"].IntValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits(this), vfx: HitVfx("vfx/vfx_slime_impact"),
                tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}
