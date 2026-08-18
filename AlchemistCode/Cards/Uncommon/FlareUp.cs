using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The threshold reader: carry a real dose and the attack pays for itself and replaces itself
[CardTheme(CardTheme.Poison)]
public class FlareUp : AlchemistCard
{
    private const int Threshold = 3;

    public FlareUp() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        WithVar("Threshold", Threshold, 0);
        WithEnergy(1, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) >= Threshold;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
        if (Dose(this) < Threshold) return;
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
