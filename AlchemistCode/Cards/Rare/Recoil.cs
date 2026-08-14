using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Hooks;
using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Recoil : AlchemistCard
{
    public Recoil() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithBlock(5, 3);
        WithKeyword(CardKeyword.Exhaust);
    }

    // The Block this gains lands before the hit, so the preview has to include it. GainBlock runs
    // Hook.ModifyBlock on the way in, so the preview has to run it too or Dexterity is missing
    private int RawDamage
    {
        get
        {
            var held = Owner?.Creature.Block ?? 0;
            if (!IsMutable || Owner?.Creature is not { } creature) return held;
            var gain = (decimal)DynamicVars.Block.IntValue;
            if ((CombatState ?? creature.CombatState) is { } combat)
                gain = Hook.ModifyBlock(combat, creature, gain, ValueProp.Move, this, null, out _);
            return held + (int)Math.Max(gain, 0m);
        }
    }

    protected override int? RawFormulaDamagePreview => RawDamage > 0 ? RawDamage : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        var damage = ApplyEnchantDamage(Owner.Creature.Block);
        if (damage <= 0) return;
        await DamageCmd.Attack(damage).FromCard(this, play)
            .WithHitFx(HitVfx("vfx/vfx_heavy_blunt"), null, "heavy_attack.mp3")
            .Targeting(play.Target!).Execute(choiceContext);
    }
}
