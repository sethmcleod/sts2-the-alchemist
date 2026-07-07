using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Permeate : AlchemistCard
{
    public Permeate() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("block", 3, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PermeatePower>(choiceContext, Owner.Creature,
            DynamicVars["block"].IntValue, Owner.Creature, this);
    }
}
