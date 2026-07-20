using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Zenith : AlchemistCard
{
    public Zenith() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var allCreatures = CombatState!.Enemies.Where(e => e.IsAlive)
            .Append(Owner.Creature);
        foreach (var creature in allCreatures)
        {
            var poison = creature.GetPowerAmount<PoisonPower>();
            if (poison > 0)
                await PowerCmd.Apply<PoisonPower>(choiceContext, creature, poison, Owner.Creature, this);
            var regen = creature.GetPowerAmount<RegenPower>();
            if (regen > 0)
                await PowerCmd.Apply<RegenPower>(choiceContext, creature, regen, Owner.Creature, this);
        }
    }
}
