using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Citrinitas : TheAlchemistCard
{
    public Citrinitas() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Rubedo>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;

        var regenAmount = Owner.Creature.GetPowerAmount<RegenPower>();
        if (regenAmount > 0)
        {
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
            if (IsUpgraded)
                await DamageCmd.Attack(regenAmount).FromCard(this).TargetingAllOpponents(CombatState).Execute(choiceContext);
            else
                await DamageCmd.Attack(regenAmount).FromCard(this).TargetingRandomOpponents(CombatState).Execute(choiceContext);
        }

        CardModel rubedo = CombatState.CreateCard<Rubedo>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(rubedo);
        await CardPileCmd.AddGeneratedCardToCombat(rubedo, PileType.Hand, Owner);
    }
}
