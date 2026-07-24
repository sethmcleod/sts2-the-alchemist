using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class RollingBoil : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public RollingBoil() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) =>
                (card.IsUpgraded ? 6m : 4m) * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, 2, vfx: HitVfx("vfx/vfx_attack_blunt"), tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
    }
}
