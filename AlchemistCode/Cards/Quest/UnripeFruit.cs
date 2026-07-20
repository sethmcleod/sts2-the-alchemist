using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Cards.Quest;

// Added to the deck by Midas Fruit. Ripens into a Golden Fruit after 4 combats, the same
// pattern as the base game's Dowsing and Guilty quest cards
public class UnripeFruit : AlchemistCard
{
    public const int MaxCombats = 4;

    private int _combatsSeen;

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public UnripeFruit() : base(-1, CardType.Quest, CardRarity.Quest, TargetType.None)
    {
        WithVar("combats", MaxCombats);
        WithKeyword(CardKeyword.Unplayable);
        // Show what this ripens into, so the wait reads as a payoff and not as a dead card
        WithTips(_ => new[] { HoverTipFactory.FromCard<GoldenFruit>() });
    }

    [SavedProperty]
    public int CombatsSeen
    {
        get => _combatsSeen;
        set
        {
            AssertMutable();
            _combatsSeen = value;
            DynamicVars["combats"].BaseValue = MaxCombats - _combatsSeen;
        }
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        if (Pile is not { Type: PileType.Deck }) return;
        CombatsSeen++;
        if (CombatsSeen < MaxCombats) return;
        PlayerCmd.CompleteQuest(this);
        await CardCmd.TransformTo<GoldenFruit>(this);
    }
}
