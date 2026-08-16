using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class SlowBurn : AlchemistCard
{
    protected override bool Ferments => true;

    public SlowBurn() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(5, static (card, _) =>
                (card.IsUpgraded ? 6m : 4m) * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
    }
}
