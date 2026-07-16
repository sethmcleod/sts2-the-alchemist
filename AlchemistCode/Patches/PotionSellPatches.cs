using System.Reflection;
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
using MegaCrit.Sts2.Core.Nodes;
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
    private static readonly FieldInfo LabelField =
        AccessTools.Field(typeof(NPotionPopupButton), "_label");

    private static MethodInfo? _setTextMethod;
    private static readonly FieldInfo PlayersField =
        AccessTools.Field(typeof(NMerchantRoom), "_players");

    private static readonly FieldInfo HoldersListField =
        AccessTools.Field(typeof(NPotionContainer), "_holders");

    private const int MaxGreetingIndex = 5;
    private static int _greetingIndex = 1;
    private static bool _soldThisVisit;

    private static bool CanSellPotions(PotionModel potion)
    {
        if (potion is FoulPotion) return false;
        var owner = potion.Owner;
        if (owner == null) return false;
        if (owner.GetRelic<WeatheredKit>() == null && owner.GetRelic<GildedKit>() == null) return false;
        return owner.RunState.CurrentRoom is MerchantRoom;
    }

    private static int GetGoldForRarity(PotionRarity rarity)
    {
        return rarity switch
        {
            PotionRarity.Common => 50,
            PotionRarity.Uncommon => 75,
            PotionRarity.Rare => 100,
            _ => 50
        };
    }

    private static void SetButtonText(NPotionPopupButton button, string text, Color? color = null)
    {
        var label = LabelField.GetValue(button);
        if (label == null) return;
        _setTextMethod ??= label.GetType().GetMethod("SetTextAutoSize",
            BindingFlags.Public | BindingFlags.Instance, null, [typeof(string)], null);
        _setTextMethod?.Invoke(label, [text]);

        if (color.HasValue && label is Control controlLabel)
            controlLabel.AddThemeColorOverride("font_color", color.Value);
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
        var gold = GetGoldForRarity(potion.Rarity);
        var owner = potion.Owner;
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
            if (potion == null) return;
            if (!CanSellPotions(potion)) return;

            var useButton = (NPotionPopupButton)UseButtonField.GetValue(__instance)!;

            var gold = GetGoldForRarity(potion.Rarity);
            var locString = new LocString("gameplay_ui", "POTION_SELL.button");
            locString.Add("Gold", gold);
            SetButtonText(useButton, locString.GetFormattedText(), new Color(0.9f, 0.77f, 0.3f));
            useButton.Enable();
        }
    }

    [HarmonyPatch(typeof(NPotionPopup), "RefreshButtons")]
    public static class PotionPopupRefreshButtonsPatch
    {
        public static void Postfix(NPotionPopup __instance)
        {
            var potion = PotionProp.GetValue(__instance) as PotionModel;
            if (potion == null) return;
            if (!CanSellPotions(potion)) return;

            var useButton = (NPotionPopupButton)UseButtonField.GetValue(__instance)!;
            useButton.Enable();
        }
    }

    [HarmonyPatch]
    public static class PotionPopupOnUseButtonPressedPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(NPotionPopup), "OnUseButtonPressed");
        }

        public static bool Prefix(NPotionPopup __instance)
        {
            var potion = PotionProp.GetValue(__instance) as PotionModel;
            if (potion == null) return true;
            if (!CanSellPotions(potion)) return true;

            var holder = (NPotionHolder)HolderField.GetValue(__instance)!;
            holder.DisableUntilPotionRemoved();

            TaskHelper.RunSafely(SellPotion(potion));

            __instance.Remove();
            return false;
        }
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

    // Atlas-safe outline: stack dark copies of the sprite behind it, offset in 8 directions. ShowBehindParent
    // keeps them under the coin, and the badge's Modulate (its fade) cascades to these children automatically
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
            AddOutline(badge, 3f);

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

    // Shop-only tooltip so players know potions can be sold here, gated exactly like the sell button
    [HarmonyPatch(typeof(PotionModel), "get_HoverTips")]
    public static class PotionSellableTipPatch
    {
        public static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!__instance.IsMutable) return; // canonical (compendium) potions have no Owner
            if (!CanSellPotions(__instance)) return;
            var tip = new HoverTip(
                new LocString("gameplay_ui", "POTION_SELL.sellable_tip.title"),
                new LocString("gameplay_ui", "POTION_SELL.sellable_tip.description"))
            { Id = SellableTipId };
            __result = __result.Append(tip);
        }
    }

    // Tint that tooltip gold. The base game only tints debuffs (red) by swapping the %Bg material, so we do
    // the same with a gold hue-shift material for our tip. Text-tip controls map 1:1 in order to the HoverTips
    [HarmonyPatch(typeof(NHoverTipSet), "Init")]
    public static class SellableTipGoldTintPatch
    {
        private static readonly FieldInfo TextContainerField =
            AccessTools.Field(typeof(NHoverTipSet), "_textHoverTipContainer");
        private static ShaderMaterial? _goldMaterial;

        // Reuse the base game's hue-shift shader (mounted at runtime); build the material in code so we don't
        // ship a .tres/shader. h/s/v tune the gold tint
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
