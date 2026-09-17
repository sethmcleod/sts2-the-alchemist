using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

// The base game's Tainted power, which only the Vital Spark affliction applies and only to the
// player. On an enemy it reads the same way: that much additional damage from every Attack until
// the end of the enemy turn, so every ally's hits count too
[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Mix)]
public class FumingMix : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public FumingMix() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
        WithPower<TaintedPower>(2, 1);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<TaintedPower>(choiceContext, target, DynamicVars["TaintedPower"].IntValue, Owner.Creature, this);
    }
}
