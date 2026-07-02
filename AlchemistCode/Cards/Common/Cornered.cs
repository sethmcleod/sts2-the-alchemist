using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Cornered : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Cornered() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // 6 (9) base; Gambit adds a flat 5 (shown via {ExtraDamage:diff()}).
        WithCalculatedDamage(6, 5, static (card, _) => ((AlchemistCard)card).IsReduced ? 1 : 0, ValueProp.Move, 3, 0);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }
}
