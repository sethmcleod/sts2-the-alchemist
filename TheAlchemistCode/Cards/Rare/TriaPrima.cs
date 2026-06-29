using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class TriaPrima : TheAlchemistCard
{
    protected override bool HasEnergyCostX => true;

    public TriaPrima() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
        WithTip(typeof(PlatingPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var x = ResolveEnergyXValue();
        var bonus = IsUpgraded ? 1 : 0;
        var amount = x + bonus;
        if (amount <= 0) return;
        foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, amount, Owner.Creature, this);
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        await PowerCmd.Apply<PlatingPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }
}
