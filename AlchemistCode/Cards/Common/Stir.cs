using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Stir : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Stir() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithTips(_ => Mixing.MixTips());
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play) =>
        Mixing.CreateChosen(choiceContext, Owner);
}
