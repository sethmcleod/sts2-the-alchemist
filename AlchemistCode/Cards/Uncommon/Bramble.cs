using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Bramble : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Block;

    public Bramble() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<ThornsPower>(3, 1);
        WithVar("ReactionThorns", 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var amount = DynamicVars["ThornsPower"].IntValue
                     + (ReactionActive ? DynamicVars["ReactionThorns"].IntValue : 0);
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }
}
