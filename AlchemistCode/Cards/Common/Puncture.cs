using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Puncture : AlchemistCard
{
    public Puncture() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithVar(new ExposedDamageVar(4).WithUpgrade(1));
        WithPower<VulnerablePower>(2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && CombatState?.Enemies.Where(e => e.IsAlive).ToList() is { Count: > 0 } enemies
        && enemies.TrueForAll(Poisoned);

    private const int Hits = 2;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is { IsAlive: true } target && Poisoned(target))
            await CommonActions.Apply<VulnerablePower>(choiceContext, this, play);
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
    }

    // The Vulnerable lands before the hits, so the number on the card counts it whenever the play
    // would: the enemy is Poisoned, is not Vulnerable yet, and has no Artifact to eat the debuff.
    // Only the preview changes. The hits read the base value through the game's hooks, by which
    // time the real Vulnerable is on the enemy or it is not
    private sealed class ExposedDamageVar : DamageVar
    {
        public ExposedDamageVar(decimal damage) : base(damage, ValueProp.Move)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            if (!runGlobalHooks || target == null || card is not Puncture { IsMutable: true, CombatState: not null })
                return;
            // In the Play pile the Vulnerable has already landed or been eaten, so the real state speaks
            if (card.Pile?.Type != PileType.Hand) return;
            if (!Poisoned(target) || target.HasPower<VulnerablePower>() || target.HasPower<ArtifactPower>())
                return;
            PreviewValue *= VulnerableMultiplier(target, card.Owner.Creature, card);
        }

        // The same three adjustments VulnerablePower makes to its own 1.5
        private static decimal VulnerableMultiplier(Creature target, Creature dealer, CardModel card)
        {
            var multiplier = ModelDb.Power<VulnerablePower>().DynamicVars["DamageIncrease"].BaseValue;
            if (dealer.Player?.GetRelic<PaperPhrog>() is { } phrog)
                multiplier = phrog.ModifyVulnerableMultiplier(target, multiplier, ValueProp.Move, dealer, card);
            if (dealer.GetPower<CrueltyPower>() is { } cruelty)
                multiplier = cruelty.ModifyVulnerableMultiplier(target, multiplier, ValueProp.Move, dealer, card);
            if (target.GetPower<DebilitatePower>() is { } debilitate)
                multiplier = debilitate.ModifyVulnerableMultiplier(target, multiplier, ValueProp.Move, dealer, card);
            return multiplier;
        }
    }
}
