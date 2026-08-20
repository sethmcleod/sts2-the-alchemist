using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;
namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Mix)]
public class FumingMix : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public FumingMix() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(1, 0);
        WithPower<VulnerablePower>(1, 0);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<WeakPower>(choiceContext, target, DynamicVars.Weak.IntValue, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, DynamicVars.Vulnerable.IntValue, Owner.Creature, this);
    }
}
