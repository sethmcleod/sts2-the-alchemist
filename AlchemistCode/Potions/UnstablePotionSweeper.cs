using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Alchemist.AlchemistCode.Potions;

// Discards every Unstable potion when combat ends. A run-hook singleton rather than a card or power
// hook, because the potions must go even if the card that made them left play, and in multiplayer
// every player's belt is swept, not only the Alchemist's.
public sealed class UnstablePotionSweeper() : CustomSingletonModel(HookType.Run)
{
    public override Task AfterCombatEnd(CombatRoom room)
    {
        foreach (var player in room.CombatState.RunState.Players)
        {
            // Copied before discarding: Discard writes back into the slot list being read
            var unstable = player.PotionSlots
                .OfType<PotionModel>()
                .Where(UnstablePotions.IsUnstable)
                .ToList();

            foreach (var potion in unstable)
                potion.Discard();
        }

        return Task.CompletedTask;
    }
}
