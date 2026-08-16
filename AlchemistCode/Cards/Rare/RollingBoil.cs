using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class RollingBoil : AlchemistCard
{
    protected override bool Ferments => true;

    public RollingBoil() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) =>
                (card.IsUpgraded ? 9m : 6m) * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, 2, vfx: HitVfx("vfx/vfx_attack_blunt"), tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
    }
}
