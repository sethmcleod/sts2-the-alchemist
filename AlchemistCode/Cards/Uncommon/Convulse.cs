using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Convulse : AlchemistCard
{
    public Convulse() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(6, 3);
        WithPower<PoisonPower>(2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target != null)
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, play.Target, DynamicVars.Poison.BaseValue, Owner.Creature, this);
            // Trigger Poison: deal damage equal to target's Poison, then reduce by 1
            var poison = play.Target.GetPowerAmount<PoisonPower>();
            if (poison > 0)
            {
                await CreatureCmd.Damage(choiceContext, play.Target, poison,
                    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner.Creature, this, null);
                await PowerCmd.Apply<PoisonPower>(choiceContext, play.Target, -1, Owner.Creature, this);
            }
        }
    }
}
