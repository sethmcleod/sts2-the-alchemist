using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Circulation : AlchemistCard
{
    public Circulation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Regen", 2, 1);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var lost = Owner.Creature.GetPowerAmount<RegenPower>();
        if (lost > 0)
        {
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
            await CardPileCmd.Draw(choiceContext, lost, Owner);
        }
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
            DynamicVars["Regen"].IntValue, Owner.Creature, this);
    }
}
