using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Decant : AlchemistCard
{
    public Decant() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        await AlchemistCardCmd.GiveCard<Distillate>(this, 1, forceUpgrade: IsUpgraded);
    }
}
