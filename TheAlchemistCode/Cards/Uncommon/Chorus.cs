using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheAlchemist.TheAlchemistCode.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Chorus : TheAlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Chorus() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
        WithVar("turns", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var allies = from c in CombatState!.GetTeammatesOf(Owner.Creature)
            where c != null && c.IsAlive && c.IsPlayer
            select c;
        foreach (var ally in allies)
            await PowerCmd.Apply<ChorusPower>(choiceContext, ally, DynamicVars["turns"].BaseValue, Owner.Creature, this);
    }
}
