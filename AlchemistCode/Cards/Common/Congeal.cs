using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Congeal : AlchemistCard
{
    protected override bool IsMettleCard => true;

    public Congeal() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(6, 1, (card, _) =>
            card.Owner.Creature.GetPowerAmount<PoisonPower>(), ValueProp.Move, 3, 0);
        WithPower<RegenPower>(2, 1); // Mettle bonus
        WithTip(typeof(PoisonPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Mettle) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (IsReduced)
            await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
