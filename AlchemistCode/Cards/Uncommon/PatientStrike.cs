using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class PatientStrike : AlchemistCard
{
    protected override bool Ferments => true;

    public PatientStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(7, static (card, _) =>
                (card.IsUpgraded ? 6m : 4m) * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 2, 0);
        WithKeyword(CardKeyword.Retain);
        WithTags(CardTag.Strike);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"),
            tmpSfx: "heavy_attack.mp3").Execute(choiceContext);
    }
}
