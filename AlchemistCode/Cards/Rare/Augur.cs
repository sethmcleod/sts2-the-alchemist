using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Augur : AlchemistCard
{
    protected override bool IsMettleCard => true;

    public Augur() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(8, 6, static (card, _) => ((AlchemistCard)card).IsReduced ? 1 : 0, ValueProp.Move, 4, 0);
        WithPower<WeakPower>(1, 1);
        WithKeyword(CardKeyword.Innate);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Mettle) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<WeakPower>(choiceContext, this, play);
    }
}
