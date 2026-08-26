using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Meltdown : AlchemistCard
{
    public Meltdown() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(28, 8);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }

    // The discount lives on the card, so it works from any combat pile, the way base Melancholy
    // listens for deaths. Same tick shape as CallusPower: the absorbed slice still counts
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (Pile is not { IsCombatPile: true }) return;
        if (target != Owner.Creature) return;
        var tick = result.UnblockedDamage + AntitoxinRules.TickAbsorb(Owner.Creature);
        if (tick <= 0) return;
        if (!AntitoxinRules.IsPoisonTick(Owner.Creature, tick, props, dealer, cardSource)) return;
        if (EnergyCost.GetResolved() <= 0) return;
        EnergyCost.AddThisCombat(-1);
    }
}
