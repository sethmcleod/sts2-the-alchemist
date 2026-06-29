using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Flush : TheAlchemistCard
{
    public Flush() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (Owner.Creature.HasPower<RegenPower>())
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
        var drawCount = regen + (IsUpgraded ? 1 : 0);
        if (drawCount > 0)
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }
}
