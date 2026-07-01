using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Siphon : AlchemistCard
{
    public Siphon() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var debuffCount = Owner.Creature.Powers.Count(p => p.TypeForCurrentAmount == PowerType.Debuff);
        if (debuffCount > 0)
            await CardPileCmd.Draw(choiceContext, debuffCount, Owner);
    }
}
