using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Deplete : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public Deplete() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(14, 4);
        WithTip(typeof(StrengthPower));
        WithTip(typeof(PoisonPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Seep) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target != null)
            await PowerCmd.Apply<DepleteStrengthDownPower>(choiceContext, play.Target, 6, Owner.Creature, this);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 3, Owner.Creature, this);
    }
}
