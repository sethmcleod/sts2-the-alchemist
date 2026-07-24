using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Vivisect : AlchemistCard
{
    public Vivisect() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithVar("Cards", 1, 0);
        WithTip(typeof(WeakPower));
        WithTip(typeof(VulnerablePower));
    }

    internal override bool GainsEffectWhenEnchanted => true;

    // The same visible-debuff count that Aggravate uses
    private static int UniqueDebuffs(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target) =>
        target?.Powers.Count(p => p.TypeForCurrentAmount == PowerType.Debuff
                                  && p.IsVisible && p.Amount > 0) ?? 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        // Draw before the Enchanted debuffs land, so this play's applications pay off on the NEXT play
        await CardPileCmd.Draw(choiceContext,
            DynamicVars["Cards"].IntValue + UniqueDebuffs(play.Target), Owner);
        if (IsEnchanted && play.Target is { IsAlive: true })
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, play.Target, 1, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, play.Target, 1, Owner.Creature, this);
        }
    }
}
