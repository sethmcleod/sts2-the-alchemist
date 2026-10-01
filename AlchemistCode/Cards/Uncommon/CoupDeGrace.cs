using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class CoupDeGrace : AlchemistCard
{
    public CoupDeGrace() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar("Per", 4, 0);
        WithCalculatedDamage(7, 4, static (card, target) =>
                Math.Floor((decimal)(target?.GetPowerAmount<PoisonPower>() ?? 0) / card.DynamicVars["Per"].BaseValue),
            ValueProp.Move, 3, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
    }
}
