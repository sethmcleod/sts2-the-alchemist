using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Infuse)]
public class Refine : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Refine() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play) =>
        Infusion.InfuseChosenFromHand(choiceContext, this, Owner, 0, AnyNumber);
}
