using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Decant)]
public class Wallop : AlchemistCard
{
    public Wallop() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(12, 4);
        WithVar("DecantMax", 5, -1);
    }

    protected override bool Decants => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!TrySpendDecant())
        {
            await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"),
                    tmpSfx: "heavy_attack.mp3").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
                .Execute(choiceContext);
            return;
        }
        var swung = false;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
        {
            var attack = CommonActions.CardAttack(this, enemy, vfx: HitVfx("vfx/vfx_heavy_blunt"),
                tmpSfx: "heavy_attack.mp3");
            // One swing animation covers the whole splash
            if (!swung) attack.WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay);
            swung = true;
            await attack.Execute(choiceContext);
        }
    }
}
