using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Anoint : AlchemistCard
{
    public Anoint() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<PoisonPower>(2, 0); // gained by you
        WithTip(typeof(PoisonPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, IsUpgraded ? 2 : 1);
    }
}
