using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Catharsis : AlchemistCard
{
    protected override bool IsFermentCard => true;

    protected override string FermentTotalText
    {
        get
        {
            if (FermentTurns <= 0) return "";
            var total = (int)DynamicVars["RegenPower"].BaseValue + (int)DynamicVars["Bonus"].BaseValue * FermentTurns;
            return $" (Gains [green]{total}[/green] Regen.)";
        }
    }

    public Catharsis() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<RegenPower>(3, 1);
        WithVar("Bonus", 2, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["RegenPower"].BaseValue
                    + DynamicVars["Bonus"].BaseValue * ConsumeFermentTurns();
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, total, Owner.Creature, this);
    }
}
