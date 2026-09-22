using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

// The Mix lane's payoff that pays into the Poison lane: one burst sized by the Mixes played so far
[CardTheme(CardTheme.Mix, CardTheme.Poison)]
public class Reclaim : AlchemistCard
{
    public Reclaim() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithCostUpgradeBy(-1);
        WithCalculatedVar("CalculatedPoison", 0, static (card, _) => Mixing.PlayedThisCombat(card.Owner));
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
        WithTips(_ => Mixing.MixRefTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        var poison = Mixing.PlayedThisCombat(Owner);
        if (poison <= 0) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, combat.HittableEnemies, poison, Owner.Creature, this);
    }
}
