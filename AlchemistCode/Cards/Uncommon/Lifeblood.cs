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
        // The number shown already includes the Regen this card is about to grant, so it reads 8 -> 10 on
        // the first play. Count that grant only when it would land, because a creature that cannot receive
        // powers never gains the Regen
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
        // Damage first: applying the Regen before the hit counts it twice, since the calculated damage
        // already includes the gain
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_bloody_impact")).Execute(choiceContext);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
