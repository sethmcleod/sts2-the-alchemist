using System.Linq;
using BaseLib.BaseLibScenes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rewards;

namespace Alchemist.AlchemistCode.Patches;

// BaseLib's linked reward set has no focus_mode, so a controller can't reach Brew's only reward
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
            // Deferred, or Godot ignores the handoff while FocusEntered is still resolving
            first?.CallDeferred(Control.MethodName.GrabFocus);
        }));
    }
}
