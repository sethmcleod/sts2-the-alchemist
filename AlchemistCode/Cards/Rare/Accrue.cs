using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Accrue : AlchemistCard
{
    private const int Hits = 3;

    protected override bool IsSeepCard => true;

    public Accrue() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithTip(typeof(PoisonPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Seep) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(Hits)
            .FromCard(this)
            .Targeting(play.Target!)
            .Execute(choiceContext);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
