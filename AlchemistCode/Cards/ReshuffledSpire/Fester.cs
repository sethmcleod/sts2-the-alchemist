using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Poison)]
public class Fester : AlchemistReshuffledCard
{
    public Fester() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<PoisonPower>(2, 1);
        WithVar("Triggers", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
        for (var i = 0; i < DynamicVars["Triggers"].IntValue; i++)
        {
            if (!target.IsAlive || target.GetPower<PoisonPower>() is not { } poison) break;
            await poison.Trigger();
        }
    }
}
