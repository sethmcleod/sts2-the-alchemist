using Alchemist.AlchemistCode.Cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Cards.Quest;

[Pool(typeof(QuestCardPool))]
[CardTheme(CardTheme.None)]
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
        WithTip(typeof(GoldenFruit));
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
