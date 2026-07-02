using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Ancient;

public class Aureate : AlchemistCard
{
    public Aureate() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        WithDamage(12, 6);
        // Upgrading Aureate upgrades the tokens it generates; the tips follow the upgrade state.
        WithUpgradingCardTip<Dross>();
        WithUpgradingCardTip<Effluvium>();
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState!).Execute(choiceContext);
        // GiveCard upgrades the token automatically when this card is upgraded.
        await AlchemistCardCmd.GiveCard<Dross>(this);
        await AlchemistCardCmd.GiveCard<Effluvium>(this);
        await AlchemistCardCmd.GiveCard<Distillate>(this);
    }
}
