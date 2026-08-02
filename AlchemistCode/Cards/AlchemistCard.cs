using System;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Enchantments;
using Alchemist.AlchemistCode.Extensions;
using Alchemist.AlchemistCode.Patches;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards;

[Pool(typeof(AlchemistCardPool))]
public abstract class AlchemistCard : ConstructedCardModel
{
    // Sentinel max for an unbounded card selection, the same literal the base game uses. A prompt paired
    // with it must not print {Amount} or {MaxCount}, which would show the raw sentinel
    public const int AnyNumber = 999999999;

    protected AlchemistCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        WithTips(card => ((AlchemistCard)card).KeywordTips());
    }

    private IEnumerable<IHoverTip> KeywordTips()
    {
        if (IsGambitCard) yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit);
        if (IsFermentCard)
        {
            yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment);
            // The keyword names Toxic as the spoil result, so show what one actually does
            yield return HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>();
        }
        if (ShowsReactionTip)
            yield return KeywordTipFactory.Build("reaction", "ALCHEMIST-REACTION.title", ReactionTipKey);
        // The Reaction condition names a mechanic of its own, so the tip for that mechanic comes with it.
        switch (Reaction)
        {
            case ReactionCondition.Block:
                yield return HoverTipFactory.Static(StaticHoverTip.Block);
                break;
            case ReactionCondition.Exhaust:
                yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
                break;
        }
    }

    private string ReactionTipKey => Reaction switch
    {
        ReactionCondition.Attack => "ALCHEMIST-REACTION.description.attack",
        ReactionCondition.Skill => "ALCHEMIST-REACTION.description.skill",
        ReactionCondition.Power => "ALCHEMIST-REACTION.description.power",
        ReactionCondition.Exhaust => "ALCHEMIST-REACTION.description.exhaust",
        ReactionCondition.Block => "ALCHEMIST-REACTION.description.block",
        ReactionCondition.Enchanted => "ALCHEMIST-REACTION.description.enchanted",
        _ => "ALCHEMIST-REACTION.description",
    };

    // Tip text lives in static_hover_tips.json under {key}.title and {key}.description
    protected static void ExplainNumber(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar variable, string key)
        => variable.WithTooltip(key);

    // For a calculated number with no var to hang a tip on, because it is never rendered
    protected void ExplainNumber(string key) => WithTips(_ => new[] { AlchemistTips.Static(key) });

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImageOrBetaPath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Internal so the static calc-damage lambdas can read it off the card arg, capturing no instance state
    internal bool IsReduced => Gambit.IsActive(Owner?.Creature);

    internal bool IsEnchanted => Enchantment != null;

    // Drives two gold glows: the card in hand once it is Enchanted, and the card in an Infuse selection
    internal virtual bool GainsEffectWhenEnchanted => false;

    protected virtual bool IsGambitCard => false;

    protected virtual bool ConditionalGlow => false;

    // The IsMutable gate makes every glow safe on canonical models, where reading Owner throws. No card
    // needs its own guard
    protected override bool ShouldGlowGoldInternal =>
        IsMutable && AlchemistModConfig.ShowHandGlows
        && ((IsGambitCard && IsReduced) || (GainsEffectWhenEnchanted && IsEnchanted)
            || ReactionActive || ConditionalGlow);

    internal bool HpFractionInRange(double lower, double upper)
    {
        if (Owner?.Creature is not { } c || c.MaxHp <= 0) return false;
        var pct = (double)c.CurrentHp / c.MaxHp;
        return pct >= lower && pct <= upper;
    }

    protected Task LoseHp(PlayerChoiceContext choiceContext, int amount) =>
        CreatureCmd.Damage(choiceContext, Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);

    // Laced hits land as a green splat instead of the card's own impact. Play time only, because
    // Enchantment is live on the mutable combat instance
    protected string HitVfx(string vfx) => Enchantment is Laced ? "vfx/vfx_slime_impact" : vfx;

    // The green splash the base game pairs with an on-hit Poison apply, see DeadlyPoison
    protected static void PoisonSplash(Creature? target)
    {
        if (target == null) return;
        var vfx = NPoisonImpactVfx.Create(target);
        if (vfx != null) NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
    }

    // A formula-damage card has no DamageVar, and only DamageVar runs the enchantment damage hooks. Apply
    // them by hand, in the order DamageVar uses
    internal int ApplyEnchantDamage(int damage)
    {
        if (Enchantment is not { } enchantment) return damage;
        decimal value = damage;
        value += enchantment.EnchantDamageAdditive(value, ValueProp.Move);
        value *= enchantment.EnchantDamageMultiplicative(value, ValueProp.Move);
        return (int)value;
    }

    // The raw total, before any hook. The card face shows the hooked total with {FormulaDamage}
    protected virtual int? RawFormulaDamagePreview => null;

    // Hook.ModifyDamage runs the global hooks the attack command will run and the enchantment hooks
    // ApplyEnchantDamage runs, so the previewed number matches the damage that lands.
    // MultiCreatureTargeting counts an enemy power only when every target has it, correct for an AoE card
    private int? FormulaDamagePreview
    {
        get
        {
            if (RawFormulaDamagePreview is not { } raw) return null;
            if (Owner?.Creature is not { } dealer) return null;
            if ((CombatState ?? dealer.CombatState) is not { } combat) return null;
            var total = Hook.ModifyDamage(Owner.RunState, combat, null, dealer, raw, ValueProp.Move,
                this, null, ModifyDamageHookType.All, CardPreviewMode.MultiCreatureTargeting, out _);
            return (int)Math.Max(total, 0m);
        }
    }

    // The same for self-inflicted HP loss, which {FormulaHpLoss} shows in red so a card that previews
    // both reads its cost apart from its payoff
    protected virtual int? FormulaHpLossPreview => null;

    private int _fermentTurns;

    // Turns in hand before the card spoils, tuned per card because the six grow at very different rates.
    // 0 means the card does not Ferment, so a Ferment card cannot exist without declaring its peak
    protected virtual int FermentPeak => 0;

    protected bool IsFermentCard => FermentPeak > 0;

    internal bool IsFermentInline => IsFermentCard;

    internal int FermentTurns => _fermentTurns;

    protected virtual string FermentTotalText => "";

    protected virtual ReactionCondition Reaction => ReactionCondition.None;

    internal bool IsReactionCard => Reaction != ReactionCondition.None;

    // Reagent names the keyword without carrying a condition, so it needs the tip too
    protected virtual bool ShowsReactionTip => IsReactionCard;

    // The last card play this player FINISHED this turn. The card being played now has not finished yet,
    // so this is the one before it, and null means this is the turn's first card
    private CardModel? PreviousCardThisTurn =>
        Owner == null || CombatState == null
            ? null
            : CombatManager.Instance.History.CardPlaysFinished
                .LastOrDefault(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Player == Owner)
                ?.CardPlay.Card;

    // Reagent hands the next Reaction card a free trigger, so that wins before the condition is read.
    // The IsMutable gate keeps every caller safe on canonical models, where Owner throws
    internal bool ReactionActive =>
        ReactionConditionMet
        || (IsMutable && IsReactionCard && Owner != null
            && Owner.Creature.GetPowerAmount<ReactivePower>() > 0);

    // The condition on its own, with no Reagent grant folded in
    private bool ReactionConditionMet
    {
        get
        {
            if (!IsMutable || !IsReactionCard || Owner == null) return false;
            if (PreviousCardThisTurn is not { } prev) return false;
            return Reaction switch
            {
                ReactionCondition.Attack => prev.Type == CardType.Attack,
                ReactionCondition.Skill => prev.Type == CardType.Skill,
                ReactionCondition.Power => prev.Type == CardType.Power,
                ReactionCondition.Enchanted => prev.Enchantment != null,
                ReactionCondition.Exhaust => CombatManager.Instance.History.Entries
                    .OfType<CardExhaustedEntry>()
                    .Any(e => e.HappenedThisTurn(CombatState) && e.Card == prev),
                ReactionCondition.Block => CombatManager.Instance.History.Entries
                    .OfType<BlockGainedEntry>()
                    .Any(e => e.HappenedThisTurn(CombatState) && e.CardPlay?.Card == prev),
                _ => false,
            };
        }
    }

    // VeryEarly, not the plain hook: RegenPower heals and decrements in BeforeSideTurnEndEarly, so a
    // Ferment tick has to land ahead of both
    public override Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (IsFermentCard && Owner != null && participants.Contains(Owner.Creature)
            && PileType.Hand.GetPile(Owner).Cards.Contains(this))
            _fermentTurns++;
        return Task.CompletedTask;
    }

    // AFTER the turn end, deliberately. CombatManager.DoTurnEnd snapshots which cards have a turn-end
    // effect before firing any of them, so a Toxic created here misses that snapshot and does not bite
    // until the END of the next turn. That leaves one turn to either exhaust it for 1 energy or eat the 5
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!IsFermentCard || _fermentTurns <= FermentPeak) return;
        if (Owner == null || !participants.Contains(Owner.Creature)) return;
        if (CombatState is not { } combat) return;
        if (!PileType.Hand.GetPile(Owner).Cards.Contains(this)) return;

        await CardCmd.Transform(this, combat.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>(Owner));
    }

    // Playing the card spends its fermentation. This fires after OnPlay has already read the count, so
    // the play still gets full value. Without it a card played at peak would return from the discard pile
    // still ripe, making every later draw a one-turn fuse instead of a fresh ramp
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == this) _fermentTurns = 0;
        return Task.CompletedTask;
    }

    // Covers the cards that were never played. Deck cards are the same instances each combat and all of
    // them get this hook, so this covers every pile
    public override Task BeforeCombatStart()
    {
        _fermentTurns = 0;
        return Task.CompletedTask;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        if (IsFermentCard)
        {
            // Always shown, even at 0 and in the compendium: the peak is now a stat of the card, and it
            // is what makes the spoil deadline legible without spending a line of card text on it
            description.Add("FermentSuffix", $" ({_fermentTurns}/{FermentPeak})");
            description.Add("FermentTotal", FermentTotalText);
        }
        // These previews read Owner, which throws on a canonical model such as the card library
        description.Add("FormulaDamage",
            IsMutable && FormulaDamagePreview is { } d ? $" ([green]{d}[/green])" : "");
        description.Add("FormulaHpLoss",
            IsMutable && FormulaHpLossPreview is { } hp ? $" ([red]{hp}[/red])" : "");
    }

    protected string HitsLine(int hits) =>
        IsMutable && hits > 0
            ? $"\n(Hits [green]{hits}[/green] {(hits == 1 ? "time" : "times")}.)"
            : "";
}
