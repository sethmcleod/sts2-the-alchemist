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
        WithPower<PoisonPower>(0, 1);
        WithVar("SelfPoison", 1, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<WeakPower>(choiceContext, target, DynamicVars.Weak.IntValue, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, DynamicVars.Vulnerable.IntValue, Owner.Creature, this);
        // Only the upgrade carries the outward dose, so the base card stays a pure debuff Mix
        if (DynamicVars.Poison.IntValue > 0)
        {
            PoisonSplash(target);
            await PowerCmd.Apply<PoisonPower>(choiceContext, target, DynamicVars.Poison.IntValue,
                Owner.Creature, this);
        }
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
    }
}
