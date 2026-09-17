using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Fallout : AlchemistCard
{
    public Fallout() : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(7, 2);
        WithVar("poison", 3, 0);
        WithTip(typeof(PoisonPower));
    }

    private const int Hits = 2;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_sandy_impact"))
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .Execute(choiceContext);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
    }
}
