using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Mix)]
public class Tincture : AlchemistCard
{
    protected override bool Ferments => true;
    protected internal override bool PlaysCastAnimation => false;

    public Tincture() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(card => Mixing.MixTips(card.IsUpgraded));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Captured once: a Mix creation hook that ferments this card would otherwise extend the loop
        var mixes = 1 + FermentTurns;
        for (var i = 0; i < mixes; i++)
            await Mixing.CreateRandom(choiceContext, Owner, IsUpgraded, source: this);
    }
}
