using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Assay : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public Assay() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithCards(1, 0);
        WithTip(typeof(RegenPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Seep) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Draw(this, choiceContext);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
