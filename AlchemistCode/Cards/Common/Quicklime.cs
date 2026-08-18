using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

// The threshold reader: on rate naked, a full point over it when you carry any dose at all. The face
// shows the live total like every other reader
[CardTheme(CardTheme.Poison)]
public class Quicklime : AlchemistCard
{
    public Quicklime() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) => Dose(card) > 0 ? (card.IsUpgraded ? 6m : 4m) : 0m,
            ValueProp.Move, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
    }
}
