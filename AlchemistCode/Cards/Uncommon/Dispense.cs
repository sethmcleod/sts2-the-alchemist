using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// Rewards holding Antitoxin rather than dumping it, the same way Gold Leaf reads your Gold
public class Dispense : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    private const int Per = 2;

    public Dispense() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar("Poison", 1, 1);
        WithTip(typeof(AntitoxinPower));
        WithTip(typeof(PoisonPower));
    }

    private int Fuel =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<AntitoxinPower>() : 0;

    protected override bool ConditionalGlow => Fuel >= Per;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var dose = Fuel / Per * DynamicVars["Poison"].IntValue;
        if (dose <= 0 || play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, dose, Owner.Creature, this);
    }
}
