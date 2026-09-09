using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

// The Poison bonus rides on the damage hook rather than a CalculatedDamageVar: an all-enemies
// AttackCommand evaluates a calculated var once with a null target when more than one enemy
// stands, so a per-target formula only ever paid against a lone enemy. The hook runs per target
// on the real hit and on the hover preview alike. The override's signature differs between the
// game branches, so it lives in Compat/FalloutCompat.cs
[CardTheme(CardTheme.Poison)]
public partial class Fallout : AlchemistCard
{
    public Fallout() : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(7, 2);
        WithVar("Bonus", 7, 2);
        WithTip(typeof(PoisonPower));
    }

    private static bool Poisoned(Creature? target) => target?.HasPower<PoisonPower>() == true;

    private decimal BonusFor(Creature? target, CardModel? cardSource) =>
        cardSource == this && Poisoned(target) ? DynamicVars["Bonus"].IntValue : 0m;

    protected override bool ConditionalGlow =>
        IsMutable && CombatState != null && CombatState.Enemies.Any(e => e.IsAlive && Poisoned(e));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact"))
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .Execute(choiceContext);
    }
}
