using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class Overbrew : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    protected override bool HasEnergyCostX => true;

    public Overbrew() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithTips(_ => Mixing.MixTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var copies = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (copies <= 0) return;
        await Mixing.CreateChosenCopies(choiceContext, Owner, copies);
    }
}
