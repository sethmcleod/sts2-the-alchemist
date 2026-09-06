using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Mix)]
public class SparklingMix : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public SparklingMix() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithEnergy(1, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Ethereal, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}
