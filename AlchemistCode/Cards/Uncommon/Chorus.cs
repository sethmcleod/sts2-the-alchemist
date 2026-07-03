using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Chorus : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Chorus() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
        WithVar("percent", 50, 25); // 50% (75%) more damage vs Poisoned enemies, for the rest of combat
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var allies = from c in CombatState!.GetTeammatesOf(Owner.Creature)
            where c != null && c.IsAlive && c.IsPlayer
            select c;
        foreach (var ally in allies)
            await PowerCmd.Apply<ChorusPower>(choiceContext, ally, DynamicVars["percent"].BaseValue, Owner.Creature, this);
    }
}
