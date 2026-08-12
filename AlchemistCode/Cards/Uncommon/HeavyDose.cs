using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class HeavyDose : AlchemistCard
{
    public HeavyDose() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, 3, static (card, _) => card.Owner.Potions.Count(),
            ValueProp.Move, 3, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var potions = Owner.Potions.Count();
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_bloody_impact")).Execute(choiceContext);
        if (potions > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, potions, Owner.Creature, this);
    }
}
