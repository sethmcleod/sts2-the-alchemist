using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Miasma : AlchemistCard
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

    public Miasma() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<PoisonPower>(2, 1);   // base Poison to ALL enemies: 2 (3)
        WithVar("Bonus", 1, 0);          // Ferment: flat +1 Poison per fermented turn
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var total = DynamicVars.Poison.BaseValue + DynamicVars["Bonus"].BaseValue * ConsumeFermentTurns();
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, total, Owner.Creature, this);
    }
}
