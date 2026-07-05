using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Nigredo : AlchemistCard
{
    public Nigredo() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithPower<PoisonPower>(4, 1);
        WithKeyword(CardKeyword.Innate); // starts in the opening hand so the token chain isn't delayed
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Albedo>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await AlchemistCardCmd.PoisonAll(choiceContext, this);
        await AlchemistCardCmd.ShuffleIntoDeck<Albedo>(this);
    }
}
