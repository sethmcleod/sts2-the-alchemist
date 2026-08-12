using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Alchemist.AlchemistCode.Potions;

// A run-hook singleton rather than a card or power hook: the potions must go even if the card that
// made them left play, and in multiplayer every player's belt is swept, not only the Alchemist's.
public sealed class UnstablePotionSweeper() : CustomSingletonModel(HookType.Run)
{
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        foreach (var player in room.CombatState.RunState.Players)
        {
            // Copied before discarding: Discard writes back into the slot list being read
            var unstable = player.PotionSlots
                .OfType<PotionModel>()
                .Where(UnstablePotions.IsUnstable)
                .ToList();

            foreach (var potion in unstable)
            {
                Patches.UnstablePotionVfxPatch.Shake(potion);
                await Cmd.Wait(0.2f, ignoreCombatEnd: true);
                Patches.UnstablePotionVfxPatch.Burst(potion);
                potion.Discard();
                // Staggered so a full belt reads as a chain of pops rather than one noise
                await Cmd.Wait(0.15f, ignoreCombatEnd: true);
            }
        }
    }
}
