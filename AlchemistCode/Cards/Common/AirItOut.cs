using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class AirItOut : AlchemistCard
{
    public AirItOut() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithVar("cleanse", 2, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash"), sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
        var lose = Math.Min(DynamicVars["cleanse"].IntValue, Owner.Creature.GetPowerAmount<PoisonPower>());
        if (lose > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -lose, Owner.Creature, this);
    }
}
