using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class Overbrew : AlchemistCard
{
    protected override bool HasEnergyCostX => true;

    public Overbrew() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Extra", 1, 1);
        WithTip(typeof(Token.BurstingMix));
        WithTip(typeof(Token.SyrupyMix));
        WithTip(typeof(Token.ZestyMix));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var copies = ResolveEnergyXValue() + DynamicVars["Extra"].IntValue;
        if (copies <= 0) return;
        await Mixing.CreateChosenCopies(choiceContext, Owner, copies, this);
    }
}
