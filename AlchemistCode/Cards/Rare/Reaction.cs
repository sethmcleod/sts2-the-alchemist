using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using BaseLib.Utils;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Reaction : AlchemistCard
{
    public Reaction() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        // 9 (12) base + 4 (5) per potion you have.
        WithCalculatedDamage(9, 4, static (card, _) =>
            ((AlchemistCard)card).Owner.Potions.Count(), ValueProp.Move, 3, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }
}
