using System.Reflection;
using Alchemist.AlchemistCode.Badges;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Alchemist.AlchemistCode.Patches;

public static class PotionSellPatches
{
    private static readonly PropertyInfo PotionProp =
        AccessTools.Property(typeof(NPotionPopup), "Potion");
    private static readonly FieldInfo HolderField =
        AccessTools.Field(typeof(NPotionPopup), "_holder");
    private static readonly FieldInfo UseButtonField =
        AccessTools.Field(typeof(NPotionPopup), "_useButton");
    private static readonly FieldInfo DiscardButtonField =
        AccessTools.Field(typeof(NPotionPopup), "_discardButton");

    // Measured from potion_popup_expanded.png (670x780): three 203px button faces with 16px gaps, mapped to
    // the popup's control space. ExpandedHeight keeps the base 239/560 vertical scale
    private const float ButtonHeight = 87f;
    private const float ExpandedHeight = 333f;
    private static readonly float[] ButtonTops = { 48f, 142f, 235f };

    private static Texture2D? _expandedTexture;
    private static bool _expandedTextureMissingLogged;
    private static Texture2D? ExpandedTexture()
    {
        if (_expandedTexture != null) return _expandedTexture;
        _expandedTexture = ResourceLoader.Load<Texture2D>(
            "res://Alchemist/images/potions/potion_pop_up/potion_popup_expanded.png");
        if (_expandedTexture == null && !_expandedTextureMissingLogged)
        {
            _expandedTextureMissingLogged = true;
            MainFile.Logger.Info(
                "potion_popup_expanded.png is not in the pck; the sell popup keeps the base frame. "
                + "Run a full publish (not publish-fast) to import and pack it.");
        }
        return _expandedTexture;
    }

    private static readonly FieldInfo PlayersField =
        AccessTools.Field(typeof(NMerchantRoom), "_players");

    private static readonly FieldInfo HoldersListField =
        AccessTools.Field(typeof(NPotionContainer), "_holders");

    private const int MaxGreetingIndex = 5;
    private static int _greetingIndex = 1;
    private static bool _soldThisVisit;

    // True when this run's owner can sell potions at all: the config override, or one of the merchant kits.
    // Does not check the room, so it also gates the "Coveted" tip, which shows anywhere in a run
    private static bool SellingEnabledFor(Player? owner)
    {
        if (owner == null) return false;
        return AlchemistModConfig.UniversalPotionSelling
            || owner.GetRelic<WeatheredKit>() != null || owner.GetRelic<GildedKit>() != null;
    }

    // A Foul potion is sellable too: throwing it at the merchant already grants Gold, so a Sell button is the
    // same payout without the throw animation. Its Use button already reads "Throw". The Sell button only
    // appears at the merchant, the one place a sale can actually happen
    private static bool CanSellPotions(PotionModel potion)
    {
        var owner = potion.Owner;
        if (!SellingEnabledFor(owner)) return false;
        return owner!.RunState.CurrentRoom is MerchantRoom;
    }

    private static int GetGoldFor(PotionModel potion)
    {
        // A Foul potion sells for exactly its throw payout, so Throw and Sell give the same Gold
        if (potion is FoulPotion) return (int)potion.DynamicVars["Gold"].BaseValue;
        var basePrice = GetGoldForRarity(potion.Rarity);
        return basePrice * AlchemistModConfig.PotionSellPercent / 100;
    }

    private static int GetGoldForRarity(PotionRarity rarity)
    {
        return rarity switch
        {
            PotionRarity.Common => 50,
            PotionRarity.Uncommon => 75,
            PotionRarity.Rare => 100,
            PotionRarity.Event => 150,
            _ => 50
        };
    }

    private static readonly LocString[] SellLines =
    [
        new LocString("gameplay_ui", "POTION_SELL.merchant_sell_1"),
        new LocString("gameplay_ui", "POTION_SELL.merchant_sell_2"),
        new LocString("gameplay_ui", "POTION_SELL.merchant_sell_3"),
        new LocString("gameplay_ui", "POTION_SELL.merchant_sell_4"),
        new LocString("gameplay_ui", "POTION_SELL.merchant_sell_5"),
        new LocString("gameplay_ui", "POTION_SELL.merchant_sell_6"),
    ];

    private static int _sellIndex;

    private static async Task SellPotion(PotionModel potion)
    {
        var gold = GetGoldFor(potion);
        var owner = potion.Owner;
        PotionSaleCounter.RecordSale(owner);
        potion.RemoveBeforeUse();

        if (!_soldThisVisit)
        {
            _soldThisVisit = true;
            if (_greetingIndex < MaxGreetingIndex)
                _greetingIndex++;
        }

        SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
        var merchantRoom = NMerchantRoom.Instance;
        if (merchantRoom != null)
        {
            var line = SellLines[_sellIndex++ % SellLines.Length];
            merchantRoom.MerchantButton.PlayDialogue(line);
            NGame.Instance?.ScreenRumble(ShakeStrength.Medium, ShakeDuration.Short, RumbleStyle.Rumble);
        }

        await PlayerCmd.GainGold(gold, owner);
    }

    [HarmonyPatch(typeof(NPotionPopup), "_Ready")]
    public static class PotionPopupReadyPatch
    {
        public static void Postfix(NPotionPopup __instance)
        {
            var potion = PotionProp.GetValue(__instance) as PotionModel;
            if (potion == null || !CanSellPotions(potion)) return;

            var useButton = (NPotionPopupButton)UseButtonField.GetValue(__instance)!;
            try
            {
                InjectSellButton(__instance, potion, useButton);
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Error(
                    "Failed to add the potion Sell button; the popup keeps Use and Discard only: " + e);
            }
        }
    }

    private static void InjectSellButton(NPotionPopup popup, PotionModel potion, NPotionPopupButton useButton)
    {
        var discardButton = (NPotionPopupButton)DiscardButtonField.GetValue(popup)!;
        if (popup.GetNodeOrNull<TextureRect>("%Container") is not { } container) return;

        // Taller 3-slot frame, and grow the popup so the third button has room. Keep the base texture if the
        // expanded image is not imported yet, so the buttons still work before the art is packed. Grow from
        // OffsetTop: _Ready already moved the popup, so OffsetTop is non-zero and a bare OffsetBottom would
        // set the wrong height. The full-rect Container follows the popup, so the frame texture fills it
        if (ExpandedTexture() is { } frame) container.Texture = frame;
        popup.OffsetBottom = popup.OffsetTop + ExpandedHeight;

        // The Sell button is a copy of Discard, not Use, so its hover Background matches the third slot rather
        // than the tent-topped first slot. Exclude Signals so it does not inherit Discard's press handler.
        // AddChild runs its _Ready, which rebinds the Label and Background child nodes
        var sellButton = (NPotionPopupButton)discardButton.Duplicate(
            (int)(Node.DuplicateFlags.Groups | Node.DuplicateFlags.Scripts));
        sellButton.Name = "SellButton";
        container.AddChild(sellButton);

        PositionButton(useButton, ButtonTops[0]);
        PositionButton(sellButton, ButtonTops[1]);
        PositionButton(discardButton, ButtonTops[2]);

        var gold = GetGoldFor(potion);
        var loc = new LocString("gameplay_ui", "POTION_SELL.button");
        loc.Add("Gold", gold);
        SetButtonLabel(sellButton, loc.GetFormattedText(), new Color(0.9f, 0.77f, 0.3f));
        sellButton.Enable();
        sellButton.Connect(NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => OnSellPressed(popup, potion)));

        // Controller and keyboard: Up and Down move Use <-> Sell <-> Discard
        WireFocus(useButton, sellButton, discardButton);
    }

    // Pin a button to the top of the container at an absolute Y, dropping the scene's anchor fraction so the
    // taller frame does not shift it. The horizontal anchors and offsets stay as the scene set them
    private static void PositionButton(Control button, float top)
    {
        button.AnchorTop = 0f;
        button.AnchorBottom = 0f;
        button.OffsetTop = top;
        button.OffsetBottom = top + ButtonHeight;
    }

    private static void WireFocus(Control use, Control sell, Control discard)
    {
        foreach (var b in new[] { use, sell, discard })
        {
            b.FocusNeighborLeft = b.GetPath();
            b.FocusNeighborRight = b.GetPath();
        }
        use.FocusNeighborTop = use.GetPath();
        use.FocusNeighborBottom = sell.GetPath();
        sell.FocusNeighborTop = use.GetPath();
        sell.FocusNeighborBottom = discard.GetPath();
        discard.FocusNeighborTop = sell.GetPath();
        discard.FocusNeighborBottom = discard.GetPath();
    }

    // Set the label on the button's child directly, so it does not depend on the button's _Ready having
    // rebound its private _label field yet
    private static void SetButtonLabel(Node button, string text, Color color)
    {
        if (button.GetNodeOrNull<MegaLabel>("Label") is not { } label) return;
        label.SetTextAutoSize(text);
        label.AddThemeColorOverride("font_color", color);
    }

    private static void OnSellPressed(NPotionPopup popup, PotionModel potion)
    {
        var holder = (NPotionHolder)HolderField.GetValue(popup)!;
        holder.DisableUntilPotionRemoved();
        TaskHelper.RunSafely(SellPotion(potion));
        popup.Remove();
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.Launch))]
    public static class RunManagerLaunchPatch
    {
        public static void Prefix()
        {
            _greetingIndex = 1;
            _soldThisVisit = false;
        }
    }

    // Atlas-safe outline: stacked dark copies rather than a shader. ShowBehindParent keeps them under the
    // coin, and the badge's own fade cascades to them
    private static void AddOutline(TextureRect badge, float width)
    {
        Vector2[] dirs =
        [
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        ];
        foreach (var dir in dirs)
        {
            var outline = new TextureRect
            {
                Texture = badge.Texture,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = badge.ExpandMode,
                StretchMode = badge.StretchMode,
                CustomMinimumSize = badge.CustomMinimumSize,
                Size = badge.Size,
                ShowBehindParent = true,
                SelfModulate = new Color(0f, 0f, 0f, 1f),
                Position = dir * width,
            };
            badge.AddChild(outline);
        }
    }

    private static void HighlightSellablePotions()
    {
        var container = NRun.Instance?.GlobalUi?.TopBar?.PotionContainer;
        if (container == null) return;
        if (HoldersListField.GetValue(container) is not System.Collections.IEnumerable slots) return;

        const float stagger = 0.13f;
        var i = 0;
        foreach (var obj in slots)
        {
            if (obj is not NPotionHolder holder || !holder.HasPotion) continue;
            var potion = holder.Potion;
            if (potion == null) continue;
            var delay = i * stagger;
            i++;

            var baseScale = potion.Scale;
            var hop = potion.CreateTween();
            if (delay > 0f) hop.TweenInterval(delay);
            hop.TweenCallback(Callable.From(() => potion.DoBounce()));
            hop.TweenProperty(potion, "scale", baseScale * 1.15f, 0.05);
            hop.TweenProperty(potion, "scale", baseScale, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);

            const float iconSize = 40f;
            var badge = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>("res://images/atlases/ui_atlas.sprites/top_bar/top_bar_gold.tres"),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(iconSize, iconSize),
                Size = new Vector2(iconSize, iconSize),
            };
            holder.AddChild(badge);
            badge.Position = new Vector2(holder.Size.X * 0.5f - iconSize * 0.5f, holder.Size.Y + 14f);
            badge.Modulate = new Color(1f, 1f, 1f, 0f);

            var pop = badge.CreateTween();
            if (delay > 0f) pop.TweenInterval(delay);
            pop.TweenProperty(badge, "modulate:a", 1f, 0.25);
            pop.TweenInterval(1.5);
            pop.TweenProperty(badge, "modulate:a", 0f, 0.6);
            pop.TweenCallback(Callable.From(() => badge.QueueFree()));
        }
    }

    [HarmonyPatch(typeof(NMerchantRoom), "_Ready")]
    public static class MerchantRoomReadyPatch
    {
        public static void Postfix(NMerchantRoom __instance)
        {
            _soldThisVisit = false;

            var players = PlayersField.GetValue(__instance) as List<Player>;
            var player = players != null ? LocalContext.GetMe(players) : null;
            if (player == null) return;
            if (player.GetRelic<WeatheredKit>() == null && player.GetRelic<GildedKit>() == null) return;
            if (!player.Potions.Any()) return;

            var index = _greetingIndex;
            var timer = __instance.GetTree().CreateTimer(0.75);
            timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(() =>
            {
                var greeting = new LocString("gameplay_ui", $"POTION_SELL.merchant_greeting_{index}");
                __instance.MerchantButton?.PlayDialogue(greeting, 3.0);
                HighlightSellablePotions();
            }));
        }
    }

    // Stable id so the tint patch can find this tooltip's rendered control
    private const string SellableTipId = "ALCHEMIST_POTION_SELLABLE";

    // Tells players a potion can be sold. Shows anywhere in a run where selling is enabled, not only at the
    // merchant, but never in the compendium, whose canonical potions have no Owner and no character context
    [HarmonyPatch(typeof(PotionModel), "get_HoverTips")]
    public static class PotionSellableTipPatch
    {
        public static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!__instance.IsMutable) return; // canonical (compendium) potions throw on Owner
            if (!SellingEnabledFor(__instance.Owner)) return;
            var tip = new HoverTip(
                new LocString("gameplay_ui", "POTION_SELL.sellable_tip.title"),
                new LocString("gameplay_ui", "POTION_SELL.sellable_tip.description"))
            { Id = SellableTipId };
            __result = __result.Append(tip);
        }
    }

    // The base game tints only debuffs, red, by swapping the %Bg material. Same method, gold material.
    // The text-tip controls map 1:1 and in order to the HoverTips
    [HarmonyPatch(typeof(NHoverTipSet), "Init")]
    public static class SellableTipGoldTintPatch
    {
        private static readonly FieldInfo TextContainerField =
            AccessTools.Field(typeof(NHoverTipSet), "_textHoverTipContainer");
        private static ShaderMaterial? _goldMaterial;

        // Built in code against the base game's own hue-shift shader, so the mod ships no .tres or shader
        private static ShaderMaterial GoldMaterial()
        {
            if (_goldMaterial != null) return _goldMaterial;
            _goldMaterial = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>("res://shaders/hsv.gdshader") };
            _goldMaterial.SetShaderParameter("h", 0.54f);
            _goldMaterial.SetShaderParameter("s", 2.4f);
            _goldMaterial.SetShaderParameter("v", 1.0f);
            return _goldMaterial;
        }

        public static void Postfix(NHoverTipSet __instance, IEnumerable<IHoverTip> hoverTips)
        {
            var index = -1;
            var ourIndex = -1;
            foreach (var tip in IHoverTip.RemoveDupes(hoverTips))
            {
                if (tip is not HoverTip hoverTip) continue;
                index++;
                if (hoverTip.Id == SellableTipId) { ourIndex = index; break; }
            }
            if (ourIndex < 0) return;
            if (TextContainerField.GetValue(__instance) is not Node container) return;
            if (ourIndex >= container.GetChildCount()) return;
            if (container.GetChild(ourIndex) is not Control control) return;
            if (control.GetNodeOrNull<CanvasItem>("%Bg") is not { } bg) return;
            bg.Material = GoldMaterial();
        }
    }
}
