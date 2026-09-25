using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Hero;

// Tear Asunder is the base shape: the combat history is the counter, so the hit count survives a
// mid-combat reload. The count is cached until the history grows, because the preview asks for it on
// every hand refresh. The cache records which instance filled it, so a clone recounts for itself
[CardTheme(CardTheme.Poison)]
public class MuscleMemory : AlchemistHeroCard
{
    private const int BaseHits = 2;

    public MuscleMemory() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(3, 1);
        WithCalculatedVar("CalculatedHits", 0, static (card, _) => Hits(card));
    }

    private static int Hits(CardModel card) => BaseHits + PlaysThisCombat(card);

    private object? _countedFor;
    private object? _countedHistory;
    private int _countedEntries = -1;
    private int _plays;

    // The play in progress is not finished yet, so the count is the earlier plays only
    private static int PlaysThisCombat(CardModel card)
    {
        if (card is not MuscleMemory self || !card.IsMutable
            || CombatManager.Instance is not { IsInProgress: true } combat) return 0;
        var history = combat.History;
        var entries = history.Entries.Count();
        if (ReferenceEquals(self._countedFor, self) && ReferenceEquals(self._countedHistory, history)
            && self._countedEntries == entries)
            return self._plays;
        self._plays = history.CardPlaysFinished.Count(e => e.CardPlay.Card == card);
        self._countedFor = self;
        self._countedHistory = history;
        self._countedEntries = entries;
        return self._plays;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits(this), vfx: HitVfx("vfx/vfx_attack_blunt"),
                tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}
