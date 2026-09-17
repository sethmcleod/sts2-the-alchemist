using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Mash : AlchemistCard
{
    public Mash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 1);
        WithUpgradingCardTip<Token.BurstingMix>(static (tip, _) => tip.AddKeyword(CardKeyword.Retain));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        var mix = await Mixing.CreateOne<Token.BurstingMix>(choiceContext, Owner, IsUpgraded, this);
        if (mix == null) return;
        CardCmd.ApplyKeyword(mix, CardKeyword.Retain);
    }
}
