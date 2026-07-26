using System;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Enchantments;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Helpers;
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
        if (IsFermentCard) yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment);
        if (IsSeepCard) yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Seep);
    }

    // Tip text lives in static_hover_tips.json under {key}.title and {key}.description
    protected static void ExplainNumber(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar variable, string key)
        => variable.WithTooltip(key);

    // For a calculated number with no var to hang a tip on, because it is never rendered
    protected void ExplainNumber(string key) =>
        WithTips(_ => new IHoverTip[]
        {
            new HoverTip(new LocString("static_hover_tips", key + ".title"),
                new LocString("static_hover_tips", key + ".description")),
        });

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImageOrBetaPath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Internal so the static calc-damage lambdas can read it off the card arg, capturing no instance state
    internal bool IsReduced => Owner?.Creature is { } c && c.CurrentHp * 2 <= c.MaxHp;

    internal bool IsEnchanted => Enchantment != null;

    // Drives two gold glows: the card in hand once it is Enchanted, and the card in an Infuse selection
    internal virtual bool GainsEffectWhenEnchanted => false;

    protected virtual bool IsGambitCard => false;

    protected virtual bool ConditionalGlow => false;

    // The IsMutable gate makes every glow safe on canonical models, where reading Owner throws. No card
    // needs its own guard
    protected override bool ShouldGlowGoldInternal =>
        IsMutable && AlchemistModConfig.ShowHandGlows
        && ((IsGambitCard && IsReduced) || (GainsEffectWhenEnchanted && IsEnchanted) || ConditionalGlow);

    // Green means "leave this in hand", the opposite signal to gold, so gold wins when a card is both.
    // SeepGlowPatches reads this
    internal bool ShouldGlowSeep =>
        IsMutable && AlchemistModConfig.ShowHandGlows && IsSeepCard && !ShouldGlowGold && !ShouldGlowRed;

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

    protected virtual bool IsFermentCard => false;

    internal bool IsFermentInline => IsFermentCard;

    internal int FermentTurns => _fermentTurns;

    protected virtual string FermentTotalText => "";

    protected virtual bool IsSeepCard => false;

    protected virtual Task OnSeep(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    // A Seep effect that already shows a card, such as a token that previews itself, opts out of the
    // flash to prevent a double preview
    protected virtual bool SeepPreviewsSelf => true;

    // VeryEarly, not the plain hook: RegenPower heals and decrements in BeforeSideTurnEndEarly, between
    // the two, so from the later hook a Seep that grants Regen misses this turn's heal
    public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner == null || !participants.Contains(Owner.Creature)
            || !PileType.Hand.GetPile(Owner).Cards.Contains(this))
            return;
        if (IsFermentCard) _fermentTurns++;
        if (IsSeepCard)
        {
            if (SeepPreviewsSelf) CardCmd.Preview(new[] { this });
            await OnSeep(choiceContext);
        }
    }

    // The card keeps its potency when you play it, so combat start is the only reset. Deck cards are the
    // same instances each combat and all of them get this hook, so this covers every pile
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
            description.Add("FermentSuffix", _fermentTurns > 0 ? $" ({_fermentTurns})" : "");
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
