using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

// Every Attack the owner plays this turn lands Amount more hits after it resolves. The extra hits
// are a plain card attack, not a card play, so they do not fire this hook again. Gone at the end
// of the turn like Rerun. A card that computes its damage by hand at play time (Nightcap, Proof,
// several base Attacks) has no damage variable to repeat, and BaseLib throws on it, so it is skipped
public class TwitchPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Attack || cardPlay.Player != Owner.Player) return;
        if (cardPlay.Target is { IsAlive: false } && cardPlay.Card.TargetType == TargetType.AnyEnemy) return;
        var vars = cardPlay.Card.DynamicVars;
        if (!vars.ContainsKey("CalculatedDamage") && !vars.ContainsKey("Damage")) return;
        Flash();
        await CommonActions.CardAttack(cardPlay.Card, cardPlay, (int)Amount).Execute(choiceContext);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
