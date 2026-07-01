using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Culture : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public Culture() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithVar("Bonus", 4, 1); // Ferment: damage per fermented turn
        WithKeyword(CardKeyword.Retain);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["Damage"].BaseValue
                    + DynamicVars["Bonus"].BaseValue * ConsumeFermentTurns();
        await DamageCmd.Attack(total).FromCard(this).Targeting(play.Target!).Execute(choiceContext);
    }
}
