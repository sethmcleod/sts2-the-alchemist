using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Nigredo : AlchemistCard
{
    public Nigredo() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<PoisonPower>(3, 1);
        WithKeyword(CardKeyword.Exhaust);
        // The rest of the chain, in the order it comes back around
        WithUpgradingCardTip<Albedo>();
        WithUpgradingCardTip<Citrinitas>();
        WithUpgradingCardTip<Rubedo>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await AlchemistCardCmd.PoisonAll(choiceContext, this);
        await AlchemistCardCmd.GiveCard<Albedo>(this);
    }
}
