using System.Collections.Generic;
using BaseLib.Abstracts;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

// A combat-hook singleton, like AntitoxinRules: the levels must rise on cards the player is not
// holding, so no power or card instance can own the listener. Same hook as RunoffPower and the
// base game's ArsenalPower, so "create" means exactly what those mean. The created card is
// already in a pile when the hook fires, so a freshly made Decant card counts its own birth,
// which is what lets Aged Batch feed the loop it belongs to
public sealed class DecantRules() : CustomSingletonModel(HookType.Combat)
{
    // Hand, draw and discard, not exhaust: the level keeps rising anywhere the card can return from
    private static readonly PileType[] Piles = [PileType.Hand, PileType.Draw, PileType.Discard];

    // Drawing an already-full card is the mechanic's jackpot moment; mark it with the potion
    // pickup chime so it reads apart from an ordinary draw
    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card,
        bool fromHandDraw)
    {
        if (card is AlchemistCard { IsDecantCard: true, DecantFull: true }
            && MegaCrit.Sts2.Core.Context.LocalContext.IsMine(card))
            MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play(
                "gain_potion.mp3", 0.7f, MegaCrit.Sts2.Core.Audio.Debug.PitchVariance.Small);
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator == null) return Task.CompletedTask;

        // Scoped to the creator's own piles, so nothing leaks across players in multiplayer
        foreach (var pileType in Piles)
        {
            foreach (var pileCard in pileType.GetPile(creator).Cards)
            {
                if (pileCard is AlchemistCard alchemistCard)
                    alchemistCard.AddDecant(1);
            }
        }

        return Task.CompletedTask;
    }
}
