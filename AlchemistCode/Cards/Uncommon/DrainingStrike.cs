using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class DrainingStrike : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public DrainingStrike() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(14, 4);
        // A "Strike" card, so base-game strike synergies (e.g. Perfected Strike) count it
        WithTags(CardTag.Strike);
        WithTip(typeof(StrengthPower));
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"), tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
        if (play.Target != null)
            await PowerCmd.Apply<DrainingStrikeStrengthDownPower>(choiceContext, play.Target, 6, Owner.Creature, this);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
