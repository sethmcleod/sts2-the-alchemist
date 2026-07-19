using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class LashOut : AlchemistCard
{
    private const int Hits = 3;

    protected override bool IsGambitCard => true;
    protected override bool IsSeepCard => true;

    public LashOut() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = Hits + (IsReduced ? 1 : 0);
        await CommonActions.CardAttack(this, play, hits).Execute(choiceContext);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
