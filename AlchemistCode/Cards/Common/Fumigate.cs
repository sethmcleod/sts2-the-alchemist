using Alchemist.AlchemistCode;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using BaseLib.Utils;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Fumigate : AlchemistCard
{
    public Fumigate() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        // 1 damage + 1 per card in your Exhaust Pile, to all enemies (neither scales on upgrade).
        WithCalculatedDamage(1, 1, static (card, _) =>
            PileType.Exhaust.GetPile(((AlchemistCard)card).Owner).Cards.Count, ValueProp.Move, 0, 0);
        WithPower<PoisonPower>(2, 0); // Gain 2 Poison (base only — upgrade removes it entirely)
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (!IsUpgraded)
            await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
    }
}
