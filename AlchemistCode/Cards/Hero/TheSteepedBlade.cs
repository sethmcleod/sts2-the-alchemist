using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Hero;

// The Hero Expansion's Broken Blade transforms into this after five plays. Event rarity keeps it out
// of every reward roll. A held turn and a play both advance it, since fermentation persists through
// a play, and every advance drips Poison on the whole enemy side
[Pool(typeof(EventCardPool))]
[CardTheme(CardTheme.Ferment, CardTheme.Poison)]
public class TheSteepedBlade : AlchemistHeroCard
{
    // The Event pool files it under the compendium's Other section; the frame stays the Alchemist's
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<Character.AlchemistCardPool>();

    protected override bool Ferments => true;

    public TheSteepedBlade() : base(1, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy)
    {
        WithDamage(9, 3);
        WithVar("Turns", 1, 0);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
        HeroExpansion.RegisterBlade(this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        await AdvanceFerment(choiceContext, DynamicVars["Turns"].IntValue);
    }

    protected override async Task OnFermented(PlayerChoiceContext choiceContext, int turns)
    {
        if (CombatState == null) return;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
        {
            PoisonSplash(enemy);
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, FermentTurns, Owner.Creature, this);
        }
    }
}
