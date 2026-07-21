using Alchemist.AlchemistCode.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Quest;

// The ripened Unripe Fruit. One splashy payoff per run, then it exhausts. QuestCardPool keeps it out of the
// Alchemist card list and files it under the Quest category, the same as the base game's own quest cards
[Pool(typeof(QuestCardPool))]
public class GoldenFruit : AlchemistCard
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    // Only the Alchemist's Midas Fruit relic can create this, so it should look like an Alchemist card: the
    // purple frame and the gold energy icon. VisualCardPool sets the look without changing the real pool that
    // keeps it out of the card list. The base game does the same for Trash Heap cards such as Clash, which
    // are event cards that look like their origin character
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<AlchemistCardPool>();

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
