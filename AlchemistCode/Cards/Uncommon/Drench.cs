using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Drench : AlchemistCard
{
    private const int Hits = 3;

    public Drench() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithVar("Poison", 1, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        for (var i = 0; i < Hits; i++)
        {
            if (play.Target is not { IsAlive: true }) return;
            await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact"))
                .Execute(choiceContext);
            if (play.Target is not { IsAlive: true } target) return;
            PoisonSplash(target);
            await PowerCmd.Apply<PoisonPower>(choiceContext, target,
                DynamicVars["Poison"].IntValue, Owner.Creature, this);
        }
    }
}
