using Alchemist.AlchemistCode.Compat;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards;

[Pool(typeof(AlchemistCardPool))]
public abstract partial class AlchemistCard : ConstructedCardModel
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
        if (IsFermentCard)
        {
            yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment);
            // The keyword names Residue as the byproduct, so show what one actually does
            yield return HoverTipFactory.FromCard<Token.Residue>();
        }
    }

    // Tip text lives in static_hover_tips.json under {key}.title and {key}.description
    protected static void ExplainNumber(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar variable, string key)
        => variable.WithTooltip(key);

    // For a calculated number with no var to hang a tip on, because it is never rendered
    protected void ExplainNumber(string key) => WithTips(_ => new[] { AlchemistTips.Static(key) });

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImageOrBetaPath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Internal so the static calc-damage lambdas can read it off the card arg, capturing no instance state
    internal bool IsEnchanted => Enchantment != null;

    protected virtual bool ConditionalGlow => false;

    // The IsMutable gate makes every glow safe on canonical models, where reading Owner throws. No card
    // needs its own guard
    protected override bool ShouldGlowGoldInternal => IsMutable && ConditionalGlow;

    internal bool HpFractionInRange(double lower, double upper)
    {
        if (Owner?.Creature is not { } c || c.MaxHp <= 0) return false;
        var pct = (double)c.CurrentHp / c.MaxHp;
        return pct >= lower && pct <= upper;
    }

    protected Task LoseHp(PlayerChoiceContext choiceContext, int amount) =>
        GameCompat.Damage(choiceContext, Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);

    // Laced keys on IsPoweredAttack, so a card that deals its damage Unpowered can carry Laced without
    // it ever firing. Those cards keep their own impact rather than showing a splat that does nothing
    protected internal virtual bool DealsUnpoweredDamage => false;

    // Laced hits land as a green splat instead of the card's own impact. Play time only, because
    // Enchantment is live on the mutable combat instance
    protected string HitVfx(string vfx) =>
        Enchantment is Laced && !DealsUnpoweredDamage ? "vfx/vfx_slime_impact" : vfx;

    // The green splash the base game pairs with an on-hit Poison apply, see DeadlyPoison
    protected static void PoisonSplash(Creature? target)
    {
        if (target == null) return;
        var vfx = NPoisonImpactVfx.Create(target);
        if (vfx != null) NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
    }

    // The dose a reader adds to its number. Zero on the canonical model, which has no Owner, so the
    // compendium shows the base value and only the combat instance shows the live total. Every card
    // that says "equal to your Poison" reads it here so the rule has one home
    protected static decimal Dose(CardModel card) =>
        card is AlchemistCard { IsMutable: true, Owner.Creature: { } creature }
            ? creature.GetPowerAmount<PoisonPower>()
            : 0m;

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
            // Must carry the same props the attack does, or Strength and Vulnerable inflate the
            // previewed number on a card whose real hit ignores them
            var props = DealsUnpoweredDamage ? ValueProp.Move | ValueProp.Unpowered : ValueProp.Move;
            var total = GameCompat.ModifyDamage(Owner.RunState, combat, null, dealer, raw, props,
                this, null, ModifyDamageHookType.All, CardPreviewMode.MultiCreatureTargeting, out _);
            return (int)Math.Max(total, 0m);
        }
    }

    // The same for self-inflicted HP loss, which {FormulaHpLoss} shows in red so a card that previews
    // both reads its cost apart from its payoff
    protected virtual int? FormulaHpLossPreview => null;

    private int _fermentTurns;

    protected virtual bool Ferments => false;

    protected bool IsFermentCard => Ferments;

    internal bool IsFermentInline => IsFermentCard;

    internal int FermentTurns => _fermentTurns;

    internal void AdvanceFerment(int turns)
    {
        if (IsFermentCard) _fermentTurns += turns;
    }

    /// <summary>The base game reserves this for roughly 12 damage and up.</summary>
    /// <summary>Set false to keep a card snappy, as the base game does for its Defends.</summary>
    protected internal virtual bool PlaysCastAnimation => true;

    protected const string HeavyAttackAnim = "heavyAttack";

    /// <summary>44% into the 1.333s clip, matching the light swing.</summary>
    protected const float HeavyAttackDelay = 0.55f;

    protected virtual string FermentTotalText => "";

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

    // The dregs land in the Discard rather than the Hand, so they cannot bite on the turn you cash in.
    // The card itself follows: a Ferment card is reusable, and the Residue is the whole cost of the play
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        // Replay plays the card again with the same CardPlay series, so the stack holds until the
        // last play of the series or the replayed hits read a fermentation of zero
        if (!cardPlay.IsLastInSeries) return;
        _fermentTurns = 0;
        if (!IsFermentCard || Owner == null || CombatState is not { } combat) return;

        // Previewed, because the card leaving play and the Residue appearing in the Discard are two
        // separate events the player never sees happen
        var dregs = combat.CreateCard<Token.Residue>(Owner);
        var added = await CardPileCmd.Add(dregs, PileType.Discard, CardPilePosition.Bottom);
        CardCmd.PreviewCardPileAdd([added]);
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
            description.Add("FermentSuffix", $" ({_fermentTurns})");
            description.Add("FermentTotal", FermentTotalText);
        }
        // These previews read Owner, which throws on a canonical model such as the card library
        description.Add("FormulaDamage",
            IsMutable && FormulaDamagePreview is { } d ? $"\n(Deals [green]{d}[/green] damage)" : "");
        description.Add("FormulaHpLoss",
            IsMutable && FormulaHpLossPreview is { } hp ? $" ([red]{hp}[/red])" : "");
    }

    protected static string PreviewLine(string key, string variable, int count)
    {
        var loc = new LocString("cards", key);
        loc.Add(variable, count);
        return loc.GetFormattedText();
    }

    protected string HitsLine(int hits) =>
        IsMutable && hits > 0 ? PreviewLine("ALCHEMIST-HITS_LINE", "Hits", hits) : "";
}
