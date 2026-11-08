using Alchemist.AlchemistCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// Base Skills and Powers call TriggerAnim in OnPlay; ours get it here instead
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class CardAnimationPatches
{
    public static void Prefix(CardModel __instance, PlayerChoiceContext choiceContext)
    {
        if (__instance is not AlchemistCard card) return;
        if (!card.PlaysCastAnimation) return;
        if (card.Owner?.Creature is not { IsAlive: true } creature) return;

        var trigger = card.Type switch
        {
            CardType.Skill => "Cast",
            CardType.Power => "PowerUp",
            _ => null,
        };
        if (trigger == null) return;

        var delay = card.Type == CardType.Power
            ? card.Owner.Character.PowerUpAnimDelay
            : card.Owner.Character.CastAnimDelay;

        TaskHelper.RunSafely(CreatureCmd.TriggerAnim(creature, trigger, delay));
    }
}
