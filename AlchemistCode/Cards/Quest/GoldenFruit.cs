using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Quest;

// The ripened Unripe Fruit. One splashy payoff per run, then it exhausts
public class GoldenFruit : AlchemistCard
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public GoldenFruit() : base(1, CardType.Skill, CardRarity.Quest, TargetType.Self)
    {
        WithVar("heal", 8);
        WithVar("gold", 25);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["heal"].BaseValue);
        await PlayerCmd.GainGold(DynamicVars["gold"].BaseValue, Owner);
        // The base game's invisible extra-turn counter, the same one Ambergris applies
        await PowerCmd.Apply<AmbergrisPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
