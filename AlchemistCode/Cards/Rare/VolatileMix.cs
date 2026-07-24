using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Utils;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class VolatileMix : AlchemistCard
{
    public VolatileMix() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(10, 5, static (card, _) =>
            ((AlchemistCard)card).Owner.Potions.Count(), ValueProp.Move, 4, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_rock_shatter"), tmpSfx: "heavy_attack.mp3").Execute(choiceContext);
        if (!Owner.Potions.Any())
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
