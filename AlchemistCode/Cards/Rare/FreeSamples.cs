using System.Linq;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Mix)]
public class FreeSamples : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    public FreeSamples() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCards(2, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(Token.BurstingMix));
        WithTip(typeof(Token.SyrupyMix));
        WithTip(typeof(Token.FumingMix));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        foreach (var ally in CombatState.Players.Where(p => p != Owner && p.Creature is { IsAlive: true }))
            await Mixing.GiveRandom(choiceContext, Owner, ally);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
