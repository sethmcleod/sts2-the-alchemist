using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Neurotoxin : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Neurotoxin() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(18, 6);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(StaticHoverTip.Stun);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab"), tmpSfx: "heavy_attack.mp3").Execute(choiceContext);
        if (IsReduced && play.Target != null)
            await CreatureCmd.Stun(play.Target);
    }
}
