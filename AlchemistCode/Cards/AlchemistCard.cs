using Alchemist.AlchemistCode.Compat;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
    // Base game's sentinel; a prompt paired with it must not print {Amount} or {MaxCount}
    public const int AnyNumber = 999999999;

    protected AlchemistCard(int cost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true)
        : base(cost, type, rarity, target, showInCardLibrary)
    {
        WithTips(card => ((AlchemistCard)card).KeywordTips());
    }

    private IEnumerable<IHoverTip> KeywordTips()
    {
        if (IsFermentCard)
        {
            yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment);
        }
    }

    protected static void ExplainNumber(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar variable, string key)
        => variable.WithTooltip(key);

    protected void ExplainNumber(string key) => WithTips(_ => new[] { AlchemistTips.Static(key) });

    private string PortraitFile => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";

    private static readonly ConcurrentDictionary<string, CardArt> ArtByEntry = new();

    private CardArt Art =>
        ArtByEntry.GetOrAdd(Id.Entry, static entry => $"{entry.RemovePrefix().ToLowerInvariant()}.png".ResolveCardArt());

    internal bool ShowsBetaArt => BetaArtChoices.ShowsBeta(Id.Entry);

    internal bool HasFinalAndBetaArt => Art.HasFinalAndBeta;

    public override string CustomPortraitPath => Art.Portrait(ShowsBetaArt);
    // Timeline slots and generated-card previews read this directly, not CustomPortraitPath
    public override string PortraitPath => Art.Portrait(ShowsBetaArt);
    public override string BetaPortraitPath => $"beta/{PortraitFile}".CardImagePath();
    internal string? BigPortraitPath => Art.BigPortrait(ShowsBetaArt);

    internal bool IsEnchanted => Enchantment != null;

    protected virtual bool ConditionalGlow => false;

    protected override bool ShouldGlowGoldInternal => IsMutable && ConditionalGlow;

    protected Task LoseHp(PlayerChoiceContext choiceContext, int amount) =>
        GameCompat.Damage(choiceContext, Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);

    protected string HitVfx(string vfx) =>
        Enchantment is Laced ? "vfx/vfx_slime_impact" : vfx;

    protected static void PoisonSplash(Creature? target)
    {
        if (target == null) return;
        var vfx = NPoisonImpactVfx.Create(target);
        if (vfx != null) NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
    }

    protected static bool Poisoned(Creature? creature) => creature?.HasPower<PoisonPower>() == true;

    protected bool AllEnemiesPoisoned =>
        IsMutable && CombatState?.HittableEnemies is { Count: > 0 } enemies && enemies.All(Poisoned);

    protected static decimal Dose(CardModel card) =>
        card is AlchemistCard { IsMutable: true, Owner.Creature: { } creature }
            ? creature.GetPowerAmount<PoisonPower>()
            : 0m;

    private static readonly FieldInfo HoverTipsField = AccessTools.Field(typeof(ConstructedCardModel), "_hoverTips");

    // BaseLib's WithVars adds an amountless tip for each PowerVar; swap it for a numbered one
    protected ConstructedCardModel WithNumberedPower<T>(int baseVal, int upgrade = 0) where T : PowerModel
    {
        WithVar(new PowerVar<T>(baseVal).WithUpgrade(upgrade));
        if (HoverTipsField.GetValue(this) is List<TooltipSource> { Count: > 0 } tips) tips.RemoveAt(tips.Count - 1);
        var name = typeof(T).Name;
        return WithTips(card => new[] { HoverTipFactory.FromPower<T>(card.DynamicVars[name].IntValue) });
    }

    protected static int OtherHandCount(CardModel card)
    {
        if (card is not AlchemistCard { IsMutable: true, CombatState: not null, Owner: { } owner }) return 0;
        var count = 0;
        foreach (var other in PileType.Hand.GetPile(owner).Cards)
            if (other != card) count++;
        return count;
    }

    protected static int PoisonedEnemies(CardModel card)
    {
        if (card is not AlchemistCard { IsMutable: true, CombatState: { } combat }) return 0;
        var count = 0;
        foreach (var enemy in combat.Enemies)
            if (enemy.IsAlive && Poisoned(enemy)) count++;
        return count;
    }

    protected virtual int? RawFormulaDamagePreview => null;

    private CardPreviewMode _previewMode;
    private Creature? _previewTarget;
    private bool _previewRunsHooks;

    internal void RecordPreviewContext(CardPreviewMode mode, Creature? target, bool runsHooks)
    {
        _previewMode = mode;
        _previewTarget = target;
        _previewRunsHooks = runsHooks;
    }

    private int? FormulaDamagePreview
    {
        get
        {
            if (RawFormulaDamagePreview is not { } raw) return null;
            if (!_previewRunsHooks) return raw;
            if (Owner?.Creature is not { } dealer) return null;
            if ((CombatState ?? dealer.CombatState) is not { } combat) return null;
            var total = GameCompat.ModifyDamage(Owner.RunState, combat, _previewTarget, dealer, raw, ValueProp.Move,
                this, null, ModifyDamageHookType.All, _previewMode, out _);
            return (int)Math.Max(total, 0m);
        }
    }

    protected virtual int? FormulaHpLossPreview => null;

    protected int AntitoxinCapacity =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<Powers.AntitoxinPower>() : 0;

    private int _fermentTurns;

    protected virtual bool Ferments => false;

    protected bool IsFermentCard => Ferments;

    internal bool IsFermentInline => IsFermentCard;

    private bool HasMotherOfVinegar =>
        IsMutable && Owner is { } player
        && System.Linq.Enumerable.Any(player.Relics,
            r => r is Relics.MotherOfVinegar && !r.IsMelted);

    internal int FermentTurns => _fermentTurns + (IsFermentCard && HasMotherOfVinegar ? 1 : 0);

    protected virtual void OnFermentTurnsChanged() { }

    internal static bool IsBrewing(CardModel card) => card is AlchemistCard { IsFermentInline: true };

    internal int DrainFerment()
    {
        var turns = _fermentTurns;
        _fermentTurns = 0;
        OnFermentTurnsChanged();
        return turns;
    }

    // Raw turns without the Mother of Vinegar floor, or a pour would pay it twice
    internal bool HasStoredFerment => _fermentTurns > 0;

    internal int StoredFermentTurns => _fermentTurns;

    internal void ReceiveFerment(int turns)
    {
        if (!IsFermentCard) return;
        _fermentTurns += turns;
        OnFermentTurnsChanged();
    }

    internal async Task AdvanceFerment(PlayerChoiceContext choiceContext, int turns)
    {
        if (!IsFermentCard) return;
        _fermentTurns += turns;
        OnFermentTurnsChanged();
        if (turns <= 0 || Owner?.Creature is not { } creature) return;
        if (creature.GetPower<MellowPower>() is { } mellow)
            await mellow.OnFermented(turns);
        if (creature.GetPower<OverflowPower>() is { } overflow)
            await overflow.OnFermented(choiceContext, this, turns);
        await OnFermented(choiceContext, turns);
    }

    protected virtual Task OnFermented(PlayerChoiceContext choiceContext, int turns) => Task.CompletedTask;

    protected internal virtual bool PlaysCastAnimation => true;

    protected const string HeavyAttackAnim = "heavyAttack";

    protected const float HeavyAttackDelay = 0.55f;

    private bool FermentsThisTurn =>
        Owner is { } player && PileType.Hand.GetPile(player).Cards.Contains(this);

    // Must run before RegenPower's BeforeSideTurnEndEarly
    public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (IsFermentCard && Owner != null && participants.Contains(Owner.Creature)
            && FermentsThisTurn)
            await AdvanceFerment(choiceContext, 1);
    }

    public override Task BeforeCombatStart()
    {
        _fermentTurns = 0;
        OnFermentTurnsChanged();
        return Task.CompletedTask;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // Reading Owner throws on a canonical model (the compendium)
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
}
