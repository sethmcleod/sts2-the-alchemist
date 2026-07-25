using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Overdose : AlchemistCard
{
    public Overdose() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(15, 5);
        WithVar("hpLoss", 4, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await LoseHp(choiceContext, DynamicVars["hpLoss"].IntValue);
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_blunt")).Execute(choiceContext);
    }
}
