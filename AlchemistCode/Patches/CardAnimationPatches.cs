using Alchemist.AlchemistCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// Attacks animate on their own: DamageCmd defaults the attacker animation to "Attack". Skills and
// Powers do not, and every base card calls TriggerAnim itself at the top of OnPlay. Rather than repeat
// that for ~60 cards, it happens once here on the wrapper that invokes OnPlay
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class CardAnimationPatches
{
    public static void Prefix(CardModel __instance, PlayerChoiceContext choiceContext)
    {
        if (__instance is not AlchemistCard card) return;
        if (!card.PlaysCastAnimation) return;
        if (card.Owner?.Creature is not { IsAlive: true } creature) return;

        // Powers use their own trigger, following the base characters. Both resolve to the cast
        // animation, but keeping them distinct leaves room to split them later
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

        // Fire and forget: the wrapper is not awaiting us, and the animation runs alongside the effect
        TaskHelper.RunSafely(CreatureCmd.TriggerAnim(creature, trigger, delay));
    }
}
