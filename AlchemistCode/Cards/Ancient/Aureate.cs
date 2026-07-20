using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Ancient;

public class Aureate : AlchemistCard
{
    public Aureate() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        WithDamage(12, 6);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .TargetingAllOpponents(CombatState!).Execute(choiceContext);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 0, AnyNumber);
    }
}
