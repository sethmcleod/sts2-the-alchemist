using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Transmute : AlchemistCard
{
    public Transmute() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithVar("Cards", 2, 0);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play) =>
        Infusion.InfuseChosen(choiceContext, this, PileType.Hand, DynamicVars["Cards"].IntValue);
}
