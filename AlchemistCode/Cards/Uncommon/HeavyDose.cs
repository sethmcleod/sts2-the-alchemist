using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class HeavyDose : AlchemistCard
{
    public HeavyDose() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(20, 6);
        WithPower<PoisonPower>(4, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"),
            sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
        if (play.Target == null) return;
        PoisonSplash(play.Target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, play.Target, DynamicVars.Poison.BaseValue,
            Owner.Creature, this);
    }
}
