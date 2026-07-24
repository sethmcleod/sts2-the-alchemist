using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class NeedlePoint : AlchemistCard
{
    public NeedlePoint() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithPower<WeakPower>(1, 1);
        WithVar("Vuln", 1, 1);
        WithKeyword(CardKeyword.Innate);
        WithTip(typeof(VulnerablePower));
    }

    internal override bool GainsEffectWhenEnchanted => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        await CommonActions.Apply<WeakPower>(choiceContext, this, play);
        if (IsEnchanted)
            await PowerCmd.Apply<VulnerablePower>(choiceContext, play.Target!,
                DynamicVars["Vuln"].IntValue, Owner.Creature, this);
    }
}
