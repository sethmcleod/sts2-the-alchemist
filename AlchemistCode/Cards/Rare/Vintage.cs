using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Vintage : AlchemistCard
{
    protected override int FermentPeak => 4;

    protected override string FermentTotalText
    {
        get
        {
            if (FermentTurns <= 0) return "";
            var total = (int)DynamicVars["RegenPower"].BaseValue + (int)DynamicVars["Bonus"].BaseValue * FermentTurns;
            return PreviewLine("ALCHEMIST-GAINS_REGEN_LINE", "Amount", total);
        }
    }

    public Vintage() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<RegenPower>(3, 1);
        WithVar("Bonus", 1, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["RegenPower"].BaseValue
                    + DynamicVars["Bonus"].BaseValue * FermentTurns;
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, total, Owner.Creature, this);
    }
}
