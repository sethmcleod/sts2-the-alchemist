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
    public DrainingStrike() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(14, 0);
        WithVar("StrengthLoss", 6, 3);
        // A "Strike" card, so base-game strike synergies such as Perfected Strike count it
        WithTags(CardTag.Strike);
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"), tmpSfx: "blunt_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay).Execute(choiceContext);
        if (play.Target != null)
            await PowerCmd.Apply<DrainingStrikeStrengthDownPower>(choiceContext, play.Target, DynamicVars["StrengthLoss"].IntValue, Owner.Creature, this);
    }
}
