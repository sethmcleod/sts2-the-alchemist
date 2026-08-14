using BaseLib.Utils;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class DeepBreath : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public DeepBreath() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 0);
        WithVar("Infuse", 1, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, DynamicVars["Infuse"].IntValue);
        await CommonActions.Draw(this, choiceContext);
    }
}
