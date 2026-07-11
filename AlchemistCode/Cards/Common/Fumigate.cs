using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Fumigate : AlchemistCard
{
    public Fumigate() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithCalculatedDamage(1, 2, static (card, _) =>
            PileType.Exhaust.GetPile(((AlchemistCard)card).Owner).Cards.Count, ValueProp.Move, 0, 0);
        WithPower<PoisonPower>(3, -1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
    }
}
