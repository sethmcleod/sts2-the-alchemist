using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Powers;

public class UncorkPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Never serialized: a level only empties during a card play, and the draw lands when that play ends
    private int _pendingLevels;

    internal void NoteLevelSpent() => _pendingLevels++;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_pendingLevels == 0) return;
        var owed = _pendingLevels * (int)Amount;
        _pendingLevels = 0;
        Flash();
        await CardPileCmd.Draw(choiceContext, owed, Owner.Player);
    }
}
