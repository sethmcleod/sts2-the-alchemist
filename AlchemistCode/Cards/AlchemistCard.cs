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
        }
        if (IsDecantCard)
        {
            yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Decant);
        }
    }

    // Tip text lives in static_hover_tips.json under {key}.title and {key}.description
    protected static void ExplainNumber(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar variable, string key)
        => variable.WithTooltip(key);

    // For a calculated number with no var to hang a tip on, because it is never rendered
    protected void ExplainNumber(string key) => WithTips(_ => new[] { AlchemistTips.Static(key) });

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImageOrBetaPath();
    // With the beta fallback too: the Timeline epoch slots and the generated-card previews read
    // Portrait directly, and without it every card still on beta art shows the generic back there
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImageOrBetaPath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Internal so the static calc-damage lambdas can read it off the card arg, capturing no instance state
    internal bool IsEnchanted => Enchantment != null;

    // Default: a full Decant level is a discrete met condition, so every Decant card glows for free.
    // False for everything else; cards with their own condition override it
    protected virtual bool ConditionalGlow => DecantFull;

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

    // Mother of Vinegar is a read-time floor, not an advance: combat start, the reset after a
    // play, and cards generated mid-combat all begin at 1 through this one line, and the free
    // turn never pokes Mellow because nothing "ferments"
    private bool HasMotherOfVinegar =>
        IsMutable && Owner is { } player
        && System.Linq.Enumerable.Any(player.Relics,
            r => r is Relics.MotherOfVinegar && !r.IsMelted);

    internal int FermentTurns => _fermentTurns + (IsFermentCard && HasMotherOfVinegar ? 1 : 0);

    // Pour Over moves fermentation between cards. Raw field access on both ends, because a move is
    // not fermenting: the turns were already paid to Mellow when they were first gained
    internal int DrainFerment() { var turns = _fermentTurns; _fermentTurns = 0; return turns; }

    // Raw stored turns, without the Mother of Vinegar read-time floor: the floor follows whichever
    // card reads it, so a pour that moved it would pay it twice on the receiver
    internal bool HasStoredFerment => _fermentTurns > 0;

    internal void ReceiveFerment(int turns) { if (IsFermentCard) _fermentTurns += turns; }

    // Async because every turn of fermentation gained also pays the Mellow engine. Both the natural
    // end-of-turn tick and the Trigger cards route through here, so the payoff has one home
    internal async Task AdvanceFerment(int turns)
    {
        if (!IsFermentCard) return;
        _fermentTurns += turns;
        if (Owner?.Creature.GetPower<MellowPower>() is { } mellow)
            await mellow.OnFermented(turns);
    }

    // Decant: the level rises as you create cards, wherever this card is, and resets each combat, the
    // same combat-scoped lifecycle as fermentation (a mid-combat save-and-quit loses both). A Decant
    // card declares Decants => true and a "DecantMax" var whose UPGRADE SHRINKS the threshold
    private int _decantFill;

    protected virtual bool Decants => false;

    internal bool IsDecantCard => Decants;

    internal int DecantMaxValue => IsDecantCard ? DynamicVars["DecantMax"].IntValue : 0;

    internal bool DecantFull => IsDecantCard && IsMutable && _decantFill >= DecantMaxValue;

    // Clamped at the threshold: an overfull level reads as banked progress the rules never pay
    internal void AddDecant(int amount)
    {
        if (!IsDecantCard || amount <= 0) return;
        _decantFill = Math.Min(_decantFill + amount, DecantMaxValue);
    }

    // The play consumes a FULL level only; a partial level is untouched, so the card is never a tax.
    // A Replay series spends it once: the first play spends the level, the replays read it empty
    protected bool TrySpendDecant()
    {
        if (!DecantFull) return false;
        _decantFill = 0;
        // Uncork pays its draw when the play that spent the level finishes
        if (System.Linq.Enumerable.FirstOrDefault(
                System.Linq.Enumerable.OfType<Powers.UncorkPower>(Owner.Creature.Powers)) is { } uncork)
            uncork.NoteLevelSpent();
        return true;
    }

    /// <summary>The base game reserves this for roughly 12 damage and up.</summary>
    /// <summary>Set false to keep a card snappy, as the base game does for its Defends.</summary>
    protected internal virtual bool PlaysCastAnimation => true;

    protected const string HeavyAttackAnim = "heavyAttack";

    /// <summary>44% into the 1.333s clip, matching the light swing.</summary>
    protected const float HeavyAttackDelay = 0.55f;

    protected virtual string FermentTotalText => "";

    private bool FermentsThisTurn
    {
        get
        {
            if (Owner is not { } player) return false;
            if (PileType.Hand.GetPile(player).Cards.Contains(this)) return true;
            if (!player.Creature.HasPower<UntendedPower>()) return false;
            return PileType.Draw.GetPile(player).Cards.Contains(this)
                   || PileType.Discard.GetPile(player).Cards.Contains(this);
        }
    }

    // VeryEarly, not the plain hook: RegenPower heals and decrements in BeforeSideTurnEndEarly, so a
    // Ferment tick has to land ahead of both
    public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (IsFermentCard && Owner != null && participants.Contains(Owner.Creature)
            && FermentsThisTurn)
            await AdvanceFerment(1);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return Task.CompletedTask;
        // Replay plays the card again with the same CardPlay series, so the stack holds until the
        // last play of the series or the replayed hits read a fermentation of zero
        if (!cardPlay.IsLastInSeries) return Task.CompletedTask;
        _fermentTurns = 0;
        return Task.CompletedTask;
    }

    // Covers the cards that were never played. Deck cards are the same instances each combat and all of
    // them get this hook, so this covers every pile
    public override Task BeforeCombatStart()
    {
        _fermentTurns = 0;
        _decantFill = 0;
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
        if (IsDecantCard)
        {
            // Live fill only in combat; the compendium and reward previews show the bare threshold
            description.Add("DecantSuffix",
                IsMutable && CombatState != null ? $" ({_decantFill}/{DecantMaxValue})" : "");
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
