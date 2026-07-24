using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Lifeblood : AlchemistCard
{
    private const int RegenGain = 2;

    public Lifeblood() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // The damage includes the Regen that this card is about to grant. The shown number already has
        // the gain, for example 8 -> 10 on the first play. The real Regen applies after the hit, so the
        // total is not counted twice. Count the grant only when it would land: a creature that cannot
        // receive powers never gains the Regen, so the preview must not add it either
        WithCalculatedDamage(8, 1, (card, _) =>
        {
            var creature = card.Owner.Creature;
            var regen = creature.GetPowerAmount<RegenPower>();
            return creature.CanReceivePowers ? regen + RegenGain : regen;
        }, ValueProp.Move, 2, 0);
        WithPower<RegenPower>(RegenGain, 0);
        WithTip(typeof(RegenPower));
        ExplainNumber(DynamicVars.CalculatedDamage, "ALCHEMIST-LIFEBLOOD");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Deal the damage first, then apply the Regen. If you apply the Regen before the hit, the total
        // is counted twice, because the calculated damage already includes the RegenGain
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_bloody_impact")).Execute(choiceContext);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
