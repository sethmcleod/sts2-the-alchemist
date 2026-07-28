using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Backdraft : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Attack;

    public Backdraft() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = ReactionActive ? 2 : 1;
        await CommonActions.CardAttack(this, play, hits,
            vfx: HitVfx("vfx/vfx_rock_shatter"), tmpSfx: "heavy_attack.mp3").Execute(choiceContext);
    }
}
