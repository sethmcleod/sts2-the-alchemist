using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Rubedo : AlchemistCard
{
    public Rubedo() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithVar("Gold", 15, 5);
        WithPower<StrengthPower>(1, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Nigredo>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;

        await PlayerCmd.GainGold(DynamicVars["Gold"].BaseValue, Owner);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, this);

        await PotionCmd.TryToProcure(
            PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
            Owner);

        await AlchemistCardCmd.GiveCard<Nigredo>(this);
    }
}
