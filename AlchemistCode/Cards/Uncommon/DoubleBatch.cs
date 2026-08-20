using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class DoubleBatch : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public DoubleBatch() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Mixes", 2, 1);
        WithTips(_ => Mixing.MixTips());
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play) =>
        Mixing.CreateChosen(choiceContext, Owner, DynamicVars["Mixes"].IntValue);
}
