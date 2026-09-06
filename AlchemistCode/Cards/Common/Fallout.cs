using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Fallout : AlchemistCard
{
    public Fallout() : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithCalculatedDamage(7, static (card, target) => Poisoned(target) ? Bonus(card) : 0m, ValueProp.Move, 2);
        WithVar("Bonus", 7, 2);
        WithTip(typeof(PoisonPower));
    }

    private static bool Poisoned(Creature? target) => target?.HasPower<PoisonPower>() == true;

    private static decimal Bonus(CardModel card) => card.DynamicVars["Bonus"].IntValue;

    protected override bool ConditionalGlow =>
        IsMutable && CombatState != null && CombatState.Enemies.Any(e => e.IsAlive && Poisoned(e));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact"))
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .Execute(choiceContext);
    }
}
