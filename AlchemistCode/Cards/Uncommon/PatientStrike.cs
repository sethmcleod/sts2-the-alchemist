using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class PatientStrike : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public PatientStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) =>
                (card.IsUpgraded ? 9m : 6m) * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
        // A "Strike" card, so base-game strike synergies (e.g. Perfected Strike) count it
        WithTags(CardTag.Strike);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }
}
