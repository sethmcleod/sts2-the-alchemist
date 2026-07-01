using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;

namespace Alchemist.AlchemistCode.Cards.Ancient;

public class Aureate : AlchemistCard
{
    public Aureate() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        WithDamage(12, 6);
        WithTip(typeof(Dross));
        WithTip(typeof(Effluvium));
        WithTip(typeof(Distillate));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState!).Execute(choiceContext);
        var dross = CombatState!.CreateCard<Dross>(Owner);
        var effluvium = CombatState!.CreateCard<Effluvium>(Owner);
        var distillate = CombatState!.CreateCard<Distillate>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(dross, PileType.Hand, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(effluvium, PileType.Hand, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(distillate, PileType.Hand, Owner);
    }
}
