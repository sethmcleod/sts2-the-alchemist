using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class FreshBatch : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public FreshBatch() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 0);
        WithTips(card => Mixing.MixTips(card.IsUpgraded));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        await Mixing.CreateChosen(choiceContext, Owner, upgraded: IsUpgraded);
    }
}
