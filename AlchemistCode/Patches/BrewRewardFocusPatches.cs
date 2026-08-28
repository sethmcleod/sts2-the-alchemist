using System.Linq;
using BaseLib.BaseLibScenes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rewards;

namespace Alchemist.AlchemistCode.Patches;

// The linked-reward container (BaseLib's NCustomLinkedRewardSet, cloned from the base scene) ships
// with no focus_mode, so a controller can never move focus into it. Brew offers exactly one reward,
// the linked set, so the reward screen's default-focus lands on an unfocusable control and the only
// reachable button is Leave. The base game never shows its own linked set, so the path is untested
// upstream.
[HarmonyPatch(typeof(NCustomLinkedRewardSet), "_Ready")]
public static class BrewRewardFocusPatches
{
    public static void Postfix(NCustomLinkedRewardSet __instance)
    {
        __instance.FocusMode = Control.FocusModeEnum.All;
        __instance.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
        {
            var container = __instance.GetNodeOrNull<Control>("%RewardContainer");
            var first = container?.GetChildren().OfType<NRewardButton>()
                .FirstOrDefault(b => b.IsVisibleInTree());
            // Deferred: grabbing focus while the container's own FocusEntered is still
            // resolving makes Godot ignore the handoff
            first?.CallDeferred(Control.MethodName.GrabFocus);
        }));
    }
}
