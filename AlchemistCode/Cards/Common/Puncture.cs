using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Puncture : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Puncture() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, 2);
        WithPower<VulnerablePower>(1, 1);
        WithVar("GambitWeak", 1, 0);
        WithTip(typeof(WeakPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<VulnerablePower>(choiceContext, this, play);
        if (IsReduced)
            await PowerCmd.Apply<WeakPower>(choiceContext, play.Target!,
                DynamicVars["GambitWeak"].IntValue, Owner.Creature, this);
    }
}
