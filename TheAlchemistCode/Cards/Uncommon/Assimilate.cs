using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Assimilate : TheAlchemistCard
{
    public Assimilate() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var totalPoison = CombatState.Enemies
            .Concat(CombatState.PlayerCreatures)
            .Where(c => c.IsAlive)
            .Sum(c => c.GetPowerAmount<PoisonPower>());
        if (totalPoison > 0)
            await CreatureCmd.GainBlock(Owner.Creature, totalPoison, ValueProp.Move, play);
    }
}
