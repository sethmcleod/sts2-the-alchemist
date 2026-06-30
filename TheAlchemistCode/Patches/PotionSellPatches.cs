using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TheAlchemist.TheAlchemistCode.Relics;

namespace TheAlchemist.TheAlchemistCode.Patches;

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
    private static readonly FieldInfo DialogueField =
        AccessTools.Field(typeof(NMerchantRoom), "_dialogue");
    private static readonly FieldInfo PlayersField =
        AccessTools.Field(typeof(NMerchantRoom), "_players");

    private const int MaxGreetingIndex = 5;
    private static int _greetingIndex = 1;
    private static bool _soldThisVisit;

    private static bool CanSellPotions(PotionModel potion)
    {
        if (potion is FoulPotion) return false;
        var owner = potion.Owner;
        if (owner == null) return false;
        if (owner.GetRelic<TarnishedFlask>() == null && owner.GetRelic<GildedFlask>() == null) return false;
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

    private static async Task SellPotion(PotionModel potion)
    {
        var gold = GetGoldForRarity(potion.Rarity);
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
            var dialogue = DialogueField.GetValue(merchantRoom) as MerchantDialogueSet;
            var line = dialogue != null ? Rng.Chaotic.NextItem(dialogue.FoulPotionLines) : null;
            if (line != null)
                merchantRoom.MerchantButton.PlayDialogue(line);
            NGame.Instance?.ScreenRumble(ShakeStrength.Medium, ShakeDuration.Short, RumbleStyle.Rumble);
        }

        await PlayerCmd.GainGold(gold, potion.Owner);
    }

    [HarmonyPatch(typeof(NPotionPopup), "_Ready")]
    public static class PotionPopupReadyPatch
    {
        public static void Postfix(NPotionPopup instance)
        {
            var potion = PotionProp.GetValue(instance) as PotionModel;
            if (potion == null) return;
            if (!CanSellPotions(potion)) return;

            var useButton = (NPotionPopupButton)UseButtonField.GetValue(instance)!;

            var gold = GetGoldForRarity(potion.Rarity);
            var locString = new LocString("ui", "POTION_SELL.button");
            locString.Add("Gold", gold);
            SetButtonText(useButton, locString.GetFormattedText(), new Color(0.9f, 0.77f, 0.3f));
            useButton.Enable();
        }
    }

    [HarmonyPatch(typeof(NPotionPopup), "RefreshButtons")]
    public static class PotionPopupRefreshButtonsPatch
    {
        public static void Postfix(NPotionPopup instance)
        {
            var potion = PotionProp.GetValue(instance) as PotionModel;
            if (potion == null) return;
            if (!CanSellPotions(potion)) return;

            var useButton = (NPotionPopupButton)UseButtonField.GetValue(instance)!;
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

        public static bool Prefix(NPotionPopup instance)
        {
            var potion = PotionProp.GetValue(instance) as PotionModel;
            if (potion == null) return true;
            if (!CanSellPotions(potion)) return true;

            var holder = (NPotionHolder)HolderField.GetValue(instance)!;
            holder.DisableUntilPotionRemoved();

            TaskHelper.RunSafely(SellPotion(potion));

            instance.Remove();
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

    [HarmonyPatch(typeof(NMerchantRoom), "_Ready")]
    public static class MerchantRoomReadyPatch
    {
        public static void Postfix(NMerchantRoom instance)
        {
            _soldThisVisit = false;

            var players = PlayersField.GetValue(instance) as List<Player>;
            var player = players != null ? LocalContext.GetMe(players) : null;
            if (player == null) return;
            if (player.GetRelic<TarnishedFlask>() == null && player.GetRelic<GildedFlask>() == null) return;
            if (!player.Potions.Any()) return;

            var index = _greetingIndex;
            var timer = instance.GetTree().CreateTimer(0.75);
            timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(() =>
            {
                var greeting = new LocString("ui", $"POTION_SELL.merchant_greeting_{index}");
                instance.MerchantButton?.PlayDialogue(greeting, 3.0);
            }));
        }
    }
}
