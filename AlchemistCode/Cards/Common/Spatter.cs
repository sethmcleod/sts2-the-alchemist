using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Spatter : AlchemistCard
{
    public Spatter() : base(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
        WithDamage(3, 1);
        WithVar("hits", 4, 0);
        WithPower<PoisonPower>(2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars["hits"].IntValue)
            .FromCard(this)
            .TargetingRandomOpponents(CombatState)
            .Execute(choiceContext);
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
    }
}
