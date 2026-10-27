using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Ancient;

[CardTheme(CardTheme.Poison)]
public class Wormwood : AlchemistCard
{
    private const int Hits = 2;

    public Wormwood() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        WithCalculatedDamage(4, static (card, _) => Dose(card), ValueProp.Move, 2);
        WithKeyword(AlchemistKeywords.Laced);
        WithVar("Poison", 3, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_sandy_impact"))
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .Execute(choiceContext);
        if (CombatState == null) return;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
        {
            PoisonSplash(enemy);
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy,
                DynamicVars["Poison"].IntValue, Owner.Creature, this);
        }
    }
}
