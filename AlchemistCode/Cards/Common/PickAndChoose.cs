using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class PickAndChoose : AlchemistCard
{
    public PickAndChoose() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithVar(new ScryVar(2).WithUpgrade(1));
        WithPower<PoisonPower>(4, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Scrying.Execute(choiceContext, this);
        if (play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
    }
}
