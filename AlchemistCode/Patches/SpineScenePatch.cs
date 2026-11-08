using Alchemist.AlchemistCode.Character;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Alchemist.AlchemistCode.Patches;

// BaseLib registers these scenes with no post-conversion action. Registering again replaces the entry,
// and this postfix always runs after the first registration
[HarmonyPatch(typeof(CustomCharacterModel), nameof(CustomCharacterModel.RegisterSceneConversions))]
public static class SpineScenePatch
{
    public static void Postfix(CustomCharacterModel __instance)
    {
        if (__instance is not Character.Alchemist) return;

        // Must stay Action<Node>: RegisterSceneType casts with "as", which nulls a contravariant delegate
        NodeFactory.RegisterSceneType<Node>(
            AlchemistRestSite.ScenePath, (typeof(NRestSiteCharacter), AlchemistRestSite.Attach));
        NodeFactory.RegisterSceneType<Node>(
            AlchemistMerchant.ScenePath, (typeof(NMerchantCharacter), AlchemistMerchant.Attach));
    }
}
