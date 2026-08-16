using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The Distillate set had five one-shot generators and no engine. This is the engine, and the stack is
// the only number on it, so it follows the one-amount power rule
public class Winnow : AlchemistCard
{
    public Winnow() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Cards", 1, 1);
        WithTip(typeof(Distillate));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<WinnowPower>(choiceContext, Owner.Creature,
            DynamicVars["Cards"].IntValue, Owner.Creature, this);
    }
}
