using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.None)]
public class Wallop : AlchemistCard
{
    public Wallop() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithVar("Per", 5, 2);
    }

    private int HandCount =>
        IsMutable && Owner != null ? PileType.Hand.GetPile(Owner).Cards.Count(c => c != this) : 0;

    protected override int? RawFormulaDamagePreview =>
        IsMutable && CombatState != null ? DynamicVars["Per"].IntValue * HandCount : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } target) return;
        var damage = DynamicVars["Per"].IntValue * HandCount;
        if (damage <= 0) return;
        await CommonActions.CardAttack(this, play, target, damage, ValueProp.Move,
                vfx: HitVfx("vfx/vfx_heavy_blunt"), tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
