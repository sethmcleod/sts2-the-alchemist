using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Harvest : TheAlchemistCard
{
    public Harvest() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(10, 3);
        WithVar("rewards", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);
        var killed = attack.Results.SelectMany(r => r).Any(r => r.WasTargetKilled);
        if (killed)
            await PowerCmd.Apply<HarvestPower>(choiceContext, Owner.Creature,
                DynamicVars["rewards"].IntValue, Owner.Creature, this);
    }
}
