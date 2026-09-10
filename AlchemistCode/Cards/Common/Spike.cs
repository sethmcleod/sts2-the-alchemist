using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison, CardTheme.Mix)]
public class Spike : AlchemistCard
{
    public Spike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 1);
        WithUpgradingCardTip<Token.FumingMix>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        await Mixing.CreateOne<Token.FumingMix>(choiceContext, Owner, IsUpgraded, this);
    }
}
