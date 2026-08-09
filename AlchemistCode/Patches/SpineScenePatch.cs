using Alchemist.AlchemistCode.Character;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Alchemist.AlchemistCode.Patches;

// BaseLib registers the rest site and shop scenes for conversion, but it registers no action to run
// after each conversion, and a mod scene cannot hold the SpineSprite that either model needs.
// Registering a path again with an action replaces the earlier entry, and a postfix on the method
// that does the first registration is the one place that always runs after it
[HarmonyPatch(typeof(CustomCharacterModel), nameof(CustomCharacterModel.RegisterSceneConversions))]
public static class SpineScenePatch
{
    public static void Postfix(CustomCharacterModel __instance)
    {
        if (__instance is not Character.Alchemist) return;

        // The target type rides in the tuple and the action stays an Action<Node>, because
        // RegisterSceneType casts the action with "as Action<Node>" and a contravariant
        // Action<NRestSiteCharacter> turns into null there
        NodeFactory.RegisterSceneType<Node>(
            AlchemistRestSite.ScenePath, (typeof(NRestSiteCharacter), AlchemistRestSite.Attach));
        NodeFactory.RegisterSceneType<Node>(
            AlchemistMerchant.ScenePath, (typeof(NMerchantCharacter), AlchemistMerchant.Attach));
    }
}
