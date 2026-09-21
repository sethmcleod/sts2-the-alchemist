using System.Linq;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

// Sebastian's Experimental Serum hands this over, always upgraded. Make It So with the Regent's
// Skills swapped for Mixes: every third Mix played in a turn pulls it back into the Hand from
// whichever pile holds it, the Exhaust Pile included. Free like its model, and the draw keeps
// the Mix chain fed so the return is reachable
[CardTheme(CardTheme.Mix)]
public class Hiccup : AlchemistAncientsCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Hiccup() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
        WithBlock(6, 2);
        WithCards(1, 0);
        WithVar("Mixes", 3, 0);
        WithTips(_ => Mixing.MixRefTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.Draw(this, choiceContext);
    }

    // A Compound Mix counts as the two Mixes it was made from, as Mixing.PlayedThisCombat counts it,
    // so the check is whether this play crossed a multiple rather than landed on one
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Card.Owner != Owner || !Mixing.IsMix(play.Card) || Pile?.Type is null or PileType.Hand) return;
        if (CombatState is not { } combat || CombatManager.Instance?.History is not { } history) return;
        var played = history.CardPlaysFinished
            .Where(e => e.HappenedThisTurn(combat) && e.CardPlay.Player == Owner && Mixing.IsMix(e.CardPlay.Card))
            .Sum(e => e.CardPlay.Card is Token.CompoundMix ? 2 : 1);
        var before = played - (play.Card is Token.CompoundMix ? 2 : 1);
        var every = DynamicVars["Mixes"].IntValue;
        if (played / every > before / every)
            await CardPileCmd.Add(this, PileType.Hand);
    }
}
