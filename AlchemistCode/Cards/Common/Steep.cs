using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
        WithPower<PoisonPower>(4, 2);   // base Poison: 4 (6)
        WithVar("Bonus", 2, 0);          // Ferment: flat +2 Poison per fermented turn
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;
        var total = DynamicVars.Poison.BaseValue + DynamicVars["Bonus"].BaseValue * ConsumeFermentTurns();
        await PowerCmd.Apply<PoisonPower>(choiceContext, play.Target, total, Owner.Creature, this);
    }
}
