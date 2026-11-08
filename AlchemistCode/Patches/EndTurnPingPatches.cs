using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using HarmonyLib;
using AlchemistCharacter = Alchemist.AlchemistCode.Character.Alchemist;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace Alchemist.AlchemistCode.Patches;

// Each client picks its own line: the ping message carries nothing to sync on
[HarmonyPatch(typeof(FlavorSynchronizer), "CreateEndTurnPingDialogueIfNecessary")]
public static class EndTurnPingPoolPatch
{
    // A sibling key such as ".3.sfx" shares the prefix
    private static readonly Regex NumberedLine = new(@"\.endTurnPing\.\d+$", RegexOptions.Compiled);

    // Player is null once the peer has left, so check the run first as the original does
    public static bool Prefix(Player? player, Dictionary<Player, NSpeechBubbleVfx?> ____endTurnPingDialogues)
    {
        if (NRun.Instance == null || player?.Character is not AlchemistCharacter) return true;

        ____endTurnPingDialogues.TryGetValue(player, out var existing);
        if (existing != null && GodotObject.IsInstanceValid(existing)) existing.QueueFreeSafely();

        var bubble = NSpeechBubbleVfx.Create(PickLine(player), player.Creature, 1.5, player.Character.SpeechBubbleColor);
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);
        ____endTurnPingDialogues[player] = bubble;
        return false;
    }

    private static string PickLine(Player player)
    {
        var state = player.Creature.IsDead ? "dead" : "alive";
        var baseKey = player.Character.Id.Entry + ".banter." + state + ".endTurnPing";
        var pool = LocManager.Instance.GetTable("characters").GetLocStringsWithPrefix(baseKey + ".")
            .Where(line => NumberedLine.IsMatch(line.LocEntryKey)).ToList();
        var line = pool.Count > 0 ? Rng.Chaotic.NextItem(pool)! : new LocString("characters", baseKey);
        return line.GetFormattedText();
    }
}
