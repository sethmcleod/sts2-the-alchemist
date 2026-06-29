using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Cards.Common;

public class Cornered : TheAlchemistCard
{
    private bool IsBelowHalf => Owner.Creature.CurrentHp < Owner.Creature.MaxHp / 2m;

    protected override bool ShouldGlowGoldInternal => IsBelowHalf;

    public Cornered() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, 5, (card, target) =>
            ((Cornered)card).IsBelowHalf ? 1 : 0, ValueProp.Move, 3, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }
}
