using Alchemist.AlchemistCode.Compat;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Mix)]
public class FumingMix : AlchemistCard
{
    public FumingMix() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
        WithPower<TaintedPower>(3, 1);
        WithDamage(3, 1);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.IntValue)
            .WithHitFx(HitVfx("vfx/vfx_sandy_impact"))
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
        if (play.Target is { IsAlive: true } target)
            await PowerCmd.Apply<TaintedPower>(choiceContext, target, DynamicVars["TaintedPower"].IntValue, Owner.Creature, this);
    }
}
