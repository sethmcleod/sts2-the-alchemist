using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Mash : AlchemistCard
{
    public Mash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 1);
        WithTips(card => AlchemistTips.MixSingle(
            card.IsUpgraded ? "ALCHEMIST-BURSTING_MIX_PLUS" : "ALCHEMIST-BURSTING_MIX", "mix_bursting"));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        await Mixing.CreateOne<Token.BurstingMix>(choiceContext, Owner, IsUpgraded);
    }
}
