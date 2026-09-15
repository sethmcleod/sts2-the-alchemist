using BaseLib.Commands;
using BaseLib.Extensions;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Commands;

// BaseLib's ScryCmd.Execute step for step, with one change: the prompt. BaseLib hardcodes the base
// discard prompt, which reads the maximum as a demand ("Choose 5 cards to Discard"). Same signature
// as the original so it can be deleted once ScryCmd takes a prompt; re-diff it on every BaseLib bump
public static class Scrying
{
    private static LocString Prompt => new("cards", "ALCHEMIST-SCRY.selectionScreenPrompt");

    public static Task<ScryResult> Execute(PlayerChoiceContext choiceContext, CardModel card) =>
        Execute(choiceContext, card.Owner, card.DynamicVars.Scry().IntValue);

    public static async Task<ScryResult> Execute(PlayerChoiceContext choiceContext, Player player, int amount)
    {
        var modified = BaseLibHooks.ModifyScryAmount(player, amount, out var modifiers);
        await BaseLibHooks.AfterModifyingScryAmount(choiceContext, player, modifiers, amount, modified);
        if (modified <= 0 || player.Creature.CombatState is not { } combat) return ScryResult.Empty;

        var seen = PileType.Draw.GetPile(player).Cards.Take(modified).ToList();
        if (seen.Count == 0) return ScryResult.Empty;
        var chosen = (await CardSelectCmd.FromSimpleGrid(choiceContext, seen, player,
            new CardSelectorPrefs(Prompt, 0, seen.Count))).ToList();

        // Not CardCmd.Discard: a scry must not count for Sly. A card the pile refuses (combat ending,
        // owner dead) is not recorded as discarded either
        var discardPile = PileType.Discard.GetPile(player);
        var discarded = new List<CardModel>();
        foreach (var card in chosen)
        {
            if (!(await CardPileCmd.Add(card, discardPile)).success) continue;
            discarded.Add(card);
            CombatManager.Instance.History.CardDiscarded(combat, card);
            await Hook.AfterCardDiscarded(combat, choiceContext, card);
        }
        discardPile.InvokeContentsChanged();
        await BaseLibHooks.AfterScryed(choiceContext, player, modified, discarded.Count, seen, discarded);
        return new ScryResult(discarded);
    }
}
