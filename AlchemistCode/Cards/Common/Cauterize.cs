using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Cauterize : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Attack;

    public Cauterize() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // The multiplier is 0 or 1, so the face shows the Reaction bonus only while it would land
        WithCalculatedDamage(6, 3, static (card, _) =>
            ((AlchemistCard)card).ReactionActive ? 1 : 0, ValueProp.Move, 3, 0);
        WithPower<RegenPower>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash"), sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
