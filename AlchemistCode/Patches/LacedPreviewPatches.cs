using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Enchantments;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// A card face prints PreviewValue and colours it by comparing against EnchantedValue. The base game
// folds an enchantment's bonus into EnchantedValue on purpose, because "a card's enchantment is a part
// of the card", so a flat +3 enchantment reads as the card's new number with no highlight.
//
// That is right for a static bonus and wrong for Laced, whose bonus is the live Poison stack. Without
// this, a Laced Spores at 3 base with 2 Poison prints a plain "5" and the player cannot tell it apart
// from a card that simply deals 5. Putting the un-Laced number back into EnchantedValue leaves the
// printed total alone and turns it green, which is what a native dose card like Jab already does: its
// CalculatedDamageVar keeps the calculation base in EnchantedValue and the dosed total in PreviewValue.
[HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
public static class LacedDamagePreviewPatch
{
    // A no-op whenever Laced added nothing, because EnchantedValue already equals BaseValue there
    public static void Postfix(DamageVar __instance, CardModel card)
    {
        if (card.Enchantment is Laced) __instance.EnchantedValue = __instance.BaseValue;
    }
}

// Infuse used the plain selection screen, which shows the chosen card and nothing about what it is
// about to become. Armaments gets a before and after because it opens the hand in Mode.UpgradeSelect,
// which reveals NUpgradePreview. These two patches borrow that pane for a single-card Infuse and swap
// its "after" card from an upgraded clone to an enchanted one, reproducing what NEnchantPreview.Init
// builds for the out-of-combat deck screen. Gated to single-card picks: SelectCardInUpgradeMode
// deselects the previous card on every pick, so the pane holds exactly one
[HarmonyPatch(typeof(NPlayerHand), nameof(NPlayerHand.SelectCards))]
public static class InfuseUsesPreviewPanePatch
{
    public static void Prefix(ref NPlayerHand.Mode mode)
    {
        if (Infusion.IsPreviewingInfusion && mode == NPlayerHand.Mode.SimpleSelect)
            mode = NPlayerHand.Mode.UpgradeSelect;
    }
}

[HarmonyPatch(typeof(NUpgradePreview), "Reload")]
public static class InfusePreviewShowsEnchantPatch
{
    public static void Postfix(NUpgradePreview __instance)
    {
        if (!Infusion.IsPreviewingInfusion || __instance.Card is not { } card) return;
        if (Infusion.PreviewEnchantFor(card) is not { } enchant) return;
        var (canonical, amount) = enchant;

        // _Ready resolves the pane's halves by unique name, so the same lookup reaches the "after" half
        // without reflecting on a private field
        var after = __instance.GetNodeOrNull<Control>("%After");
        if (after == null) return;
        foreach (var child in after.GetChildren()) child.QueueFreeSafely();

        // The same construction NEnchantPreview.Init uses. IsEnchantmentPreview is what routes the
        // enchantment's delta into PreviewValue so the changed numbers highlight, instead of into
        // EnchantedValue where they would read as the card's own baseline
        if (card.CardScope?.CloneCard(card) is not { } preview) return;
        var enchantment = canonical.ToMutable();
        preview.EnchantInternal(enchantment, amount);
        preview.IsEnchantmentPreview = true;
        enchantment.ModifyCard();

        if (NCard.Create(preview) is not { } node) return;
        if (NPreviewCardHolder.Create(node, showHoverTips: true, scaleOnHover: false) is not { } holder) return;
        holder.FocusMode = Control.FocusModeEnum.None;
        after.AddChildSafely(holder);
        holder.CardNode?.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
    }
}
