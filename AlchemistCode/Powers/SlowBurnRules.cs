using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Compat;
using Alchemist.AlchemistCode.Cards.Common;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Powers;

// Not CardModel.HasTurnEndInHandEffect: the game force-discards any card using that hook once
// its effect resolves, which would cancel Retain. A combat hook reads the hand instead
public sealed class SlowBurnRules() : CustomSingletonModel(HookType.Combat)
{
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        foreach (var creature in participants.Where(c => c.IsPlayer && c.Player != null).ToList())
        {
            var player = creature.Player!;
            if (player.Creature.CombatState is not { } combat) continue;
            foreach (var card in PileType.Hand.GetPile(player).Cards.OfType<SlowBurn>().ToList())
            {
                // TargetingRandomOpponents draws from the shared RunState.Rng.CombatTargets, the
                // same stream the base game's random-target attacks use. That is only safe while
                // this hook resolves the same number of times on every client
                await DamageCmd.Attack(card.Burn)
                    .Unpowered()
                    .FromCard(card, null)
                    .TargetingRandomOpponents(combat)
                    .Execute(choiceContext);
            }
        }
    }
}
