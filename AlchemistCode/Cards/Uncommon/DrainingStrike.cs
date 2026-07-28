using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class DrainingStrike : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Attack;

    public DrainingStrike() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // The multiplier is 0 or 1, so the face shows the Reaction bonus only while it would land
        WithCalculatedDamage(14, 5, static (card, _) =>
            ((AlchemistCard)card).ReactionActive ? 1 : 0, ValueProp.Move, 4, 2);
        // A "Strike" card, so base-game strike synergies such as Perfected Strike count it
        WithTags(CardTag.Strike);
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"), tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
        if (play.Target != null)
            await PowerCmd.Apply<DrainingStrikeStrengthDownPower>(choiceContext, play.Target, 6, Owner.Creature, this);
    }
}
