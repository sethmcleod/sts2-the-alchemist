using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Steep : AlchemistCard
{
    protected override bool IsFermentCard => true;

    protected override string FermentTotalText
    {
        get
        {
            if (FermentTurns <= 0) return "";
            var total = (int)DynamicVars.Poison.BaseValue + (int)DynamicVars["Bonus"].BaseValue * FermentTurns;
            return $" (Applies [green]{total}[/green] Poison.)";
        }
    }

    public Steep() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<PoisonPower>(4, 2);
        WithVar("Bonus", 1, 1);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;
        var total = DynamicVars.Poison.BaseValue + DynamicVars["Bonus"].BaseValue * FermentTurns;
        await PowerCmd.Apply<PoisonPower>(choiceContext, play.Target, total, Owner.Creature, this);
    }
}
