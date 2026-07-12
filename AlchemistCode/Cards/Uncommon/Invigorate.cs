using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Invigorate : AlchemistCard
{
    private const int RegenGain = 2;

    public Invigorate() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // Damage folds in the Regen this card is about to grant, so the shown number already reflects the
        // gain (e.g. 8 -> 10 on first play). The real Regen lands right after the hit, so this isn't double-counted
        WithCalculatedDamage(8, 1, (card, _) =>
            card.Owner.Creature.GetPowerAmount<RegenPower>() + RegenGain, ValueProp.Move, 2, 0);
        WithPower<RegenPower>(RegenGain, 0);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Deal the Regen-inclusive damage, then apply the Regen. Applying it before the hit would double-count,
        // since the calculated damage already anticipates the +RegenGain
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
