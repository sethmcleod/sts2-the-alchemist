using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Cards.Basic;
using TheAlchemist.TheAlchemistCode.Commands;

namespace TheAlchemist.TheAlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Rubedo : TheAlchemistCard
{
    public Rubedo() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithVar("Gold", 20, 5);
        WithPower<StrengthPower>(1, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Nigredo>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;

        await PlayerCmd.GainGold(DynamicVars["Gold"].BaseValue, Owner);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, this);

        var cardToUpgrade = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
        if (cardToUpgrade != null)
            CardCmd.Upgrade(cardToUpgrade);

        await AlchemistCardCmd.GiveCard<Nigredo>(this);
    }
}
