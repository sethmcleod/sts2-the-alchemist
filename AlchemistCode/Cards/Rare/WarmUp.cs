using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Mix)]
public class WarmUp : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public WarmUp() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(1, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(card => Mixing.MixTips(card.IsUpgraded));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await Mixing.CreateRandom(choiceContext, Owner, IsUpgraded);
    }
}
