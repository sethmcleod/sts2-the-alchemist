using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class CompendiumFilterOrderPatch
{
    public static void Postfix(Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
    {
        var lastBase = ____cardPoolFilters
            .Where(pair => pair.Key is not CustomCharacterModel)
            .Select(pair => pair.Value)
            .MaxBy(filter => filter.GetIndex());
        if (lastBase?.GetParent() is not { } row) return;

        var custom = ____cardPoolFilters
            .Where(pair => pair.Key is CustomCharacterModel && pair.Value.GetParent() == row)
            .Select(pair => pair.Value)
            .OrderBy(filter => filter.GetIndex())
            .ToList();

        Node anchor = lastBase;
        foreach (var filter in custom)
        {
            MoveAfter(row, filter, anchor);
            anchor = filter;
        }
    }

    private static void MoveAfter(Node parent, Node node, Node anchor)
    {
        var target = node.GetIndex() < anchor.GetIndex() ? anchor.GetIndex() : anchor.GetIndex() + 1;
        if (node.GetIndex() != target) parent.MoveChild(node, target);
    }
}
