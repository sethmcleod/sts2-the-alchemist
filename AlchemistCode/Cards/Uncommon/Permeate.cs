using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Permeate : AlchemistCard
{
    public Permeate() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("poisonPerBlock", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PermeatePower>(choiceContext, Owner.Creature,
            DynamicVars["poisonPerBlock"].IntValue, Owner.Creature, this);
    }
}
