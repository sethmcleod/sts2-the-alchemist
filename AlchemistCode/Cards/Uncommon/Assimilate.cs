using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Assimilate : AlchemistCard
{
    public Assimilate() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var totalPoison = CombatState.Enemies
            .Concat(CombatState.PlayerCreatures)
            .Where(c => c.IsAlive)
            .Sum(c => c.GetPowerAmount<PoisonPower>());
        if (totalPoison <= 0) return;
        var times = IsUpgraded ? 2 : 1;
        for (var i = 0; i < times; i++)
            await CreatureCmd.GainBlock(Owner.Creature, totalPoison, ValueProp.Move, play);
    }
}
