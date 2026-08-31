using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Decant, CardTheme.Poison)]
public class Spike : AlchemistCard
{
    public Spike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(9, 3);
        WithVar("DecantMax", 3, -1);
        WithVar("poison", 4, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override bool Decants => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        if (!TrySpendDecant()) return;
        if (play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
    }
}
