using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Enervate : AlchemistCard
{
    protected override bool IsMettleCard => true;

    public Enervate() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // Mettle bonus: 6 base, 8 upgraded — shown via {ExtraDamage:diff()} in the loc
        WithCalculatedDamage(12, 6, static (card, _) => ((AlchemistCard)card).IsReduced ? 1 : 0, ValueProp.Move, 4, 2);
        WithPower<WeakPower>(2, 1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Mettle) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<WeakPower>(choiceContext, this, play);
    }
}
