using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class DoubleDose : AlchemistCard
{
    public DoubleDose() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 1);
        WithVar("Weak", 2, 0);
        WithTip(typeof(WeakPower));
    }

    protected override bool ConditionalGlow => IsEnchanted;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (IsEnchanted)
            await PowerCmd.Apply<WeakPower>(choiceContext, play.Target!, DynamicVars["Weak"].IntValue, Owner.Creature, this);
    }
}
