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

    // The Block this gains lands before the hit, so the preview has to include it
    private int RawDamage =>
        (Owner?.Creature.Block ?? 0) + (IsMutable ? DynamicVars.Block.IntValue : 0);

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
