using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Carapace : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public Carapace() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(8, 2);
        WithVar("Bonus", 4, 1); // Ferment: Block gained per fermented turn
        WithKeyword(CardKeyword.Retain);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["Block"].BaseValue
                    + DynamicVars["Bonus"].BaseValue * ConsumeFermentTurns();
        await CreatureCmd.GainBlock(Owner.Creature, total, ValueProp.Move, play);
    }
}
