using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Poison)]
public class Switcheroo : AlchemistReshuffledCard
{
    public Switcheroo() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } target) return;
        var mine = Owner.Creature.GetPowerAmount<PoisonPower>();
        var theirs = target.GetPowerAmount<PoisonPower>();
        PoisonSplash(target);
        await SetPoison(choiceContext, target, mine);
        await SetPoison(choiceContext, Owner.Creature, theirs);
    }

    private async Task SetPoison(PlayerChoiceContext choiceContext, Creature creature, int amount)
    {
        if (creature.GetPower<PoisonPower>() is { } poison)
        {
            if (amount != poison.Amount)
                await PowerCmd.ModifyAmount(choiceContext, poison, amount - poison.Amount, Owner.Creature, this);
        }
        else if (amount > 0)
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, creature, amount, Owner.Creature, this);
        }
    }
}
