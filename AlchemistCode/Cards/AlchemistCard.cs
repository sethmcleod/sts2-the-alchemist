using System;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards;

[Pool(typeof(AlchemistCardPool))]
public abstract class AlchemistCard : ConstructedCardModel
{
    // The max count for a card selection with no upper bound. The base game uses the same literal. A prompt
    // that goes with this count must not print {Amount} or {MaxCount}, because the count shows as-is
    public const int AnyNumber = 999999999;

    protected AlchemistCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        // Keyword hover tips follow the Is*Card flags, so a flag stays the single source of truth. BaseLib's
        // WithTips adds them. They sit before the card's own dynamic tips, so the keyword tip renders first.
        // The order below is the tie-break for a card with more than one flag: Gambit, Ferment, then Seep
        WithTips(card => ((AlchemistCard)card).KeywordTips());
    }

    private IEnumerable<IHoverTip> KeywordTips()
    {
        if (IsGambitCard) yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit);
        if (IsFermentCard) yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment);
        if (IsSeepCard) yield return HoverTipFactory.FromKeyword(AlchemistKeywords.Seep);
    }

    // Attach an explanatory tip to a calculated number, so the player can see how and why the number is
    // derived. BaseLib surfaces the tip among the card's hover tips whenever this var is present. The text
    // lives in static_hover_tips.json under {key}.title and {key}.description
    protected static void ExplainNumber(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar variable, string key)
        => variable.WithTooltip(key);

    // Same explanation, for a card whose calculated number has no var to hang a tip on (the total is
    // computed at play time and never rendered). Adds it as a plain card tip from the same loc table
    protected void ExplainNumber(string key) =>
        WithTips(_ => new IHoverTip[]
        {
            new HoverTip(new LocString("static_hover_tips", key + ".title"),
                new LocString("static_hover_tips", key + ".description")),
        });

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImageOrBetaPath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Internal so the static calc-damage lambdas can read it off the card arg (they must capture no instance state)
    internal bool IsReduced => Owner?.Creature is { } c && c.CurrentHp * 2 <= c.MaxHp;

    // True when this card currently carries an enchantment (was Infused this combat). Safe on canonical models
    internal bool IsEnchanted => Enchantment != null;

    // A card with an "If this card is Enchanted" bonus sets this. It drives two glows: the card glows gold in
    // hand once it is Enchanted (the bonus is live), and the Infuse selection glows it gold so the player can
    // see which cards reward being infused. Internal, because Infusion reads it to build the glow predicate
    internal virtual bool GainsEffectWhenEnchanted => false;

    protected virtual bool IsGambitCard => false;

    // A card with a play-time conditional bonus overrides this. The card then glows gold while the
    // condition holds, the same as a Gambit card at low HP
    protected virtual bool ConditionalGlow => false;

    // The IsMutable gate makes every glow safe on canonical models. IsReduced and ConditionalGlow read
    // Owner, which throws on a canonical model (the compendium). Each card does not need its own guard
    protected override bool ShouldGlowGoldInternal =>
        IsMutable && ((IsGambitCard && IsReduced) || (GainsEffectWhenEnchanted && IsEnchanted) || ConditionalGlow);

    // A Seep card glows green while it stays in hand, because it pays off if you do not play it. The hand
    // glow patch reads this. Gold wins when a card is both, for example a reduced Lash Out: gold is the
    // transient "play this now" signal, and the green is constant. The IsMutable gate is the same as above
    internal bool ShouldGlowSeep => IsMutable && IsSeepCard && !ShouldGlowGold && !ShouldGlowRed;

    internal bool HpFractionInRange(double lower, double upper)
    {
        if (Owner?.Creature is not { } c || c.MaxHp <= 0) return false;
        var pct = (double)c.CurrentHp / c.MaxHp;
        return pct >= lower && pct <= upper;
    }

    // "Lose N HP" is unblockable, unpowered self-damage
    protected Task LoseHp(PlayerChoiceContext choiceContext, int amount) =>
        CreatureCmd.Damage(choiceContext, Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);

    // A formula-damage card deals raw DamageCmd.Attack(decimal) and has no DamageVar. Only DamageVar runs
    // the enchantment damage hooks. This method applies them by hand, in the same order DamageVar uses
    internal int ApplyEnchantDamage(int damage)
    {
        if (Enchantment is not { } enchantment) return damage;
        decimal value = damage;
        value += enchantment.EnchantDamageAdditive(value, ValueProp.Move);
        value *= enchantment.EnchantDamageMultiplicative(value, ValueProp.Move);
        return (int)value;
    }

    // A card with a runtime damage formula has no DamageVar to preview. It gives its raw total here, before
    // the enchantment hooks and the global damage hooks. The card face shows the full total live with
    // {FormulaDamage}. The value is null if the total cannot be computed, for example in the card library
    protected virtual int? RawFormulaDamagePreview => null;

    // The preview must agree with the hit. The attack command runs the global damage hooks (Strength,
    // Vigor, Weak) when it executes, and ApplyEnchantDamage runs the enchantment hooks by hand.
    // Hook.ModifyDamage runs both, so the number on the card matches the damage that lands.
    // MultiCreatureTargeting is the mode the game uses for a card face. It counts a power on the enemy only
    // when every target has that power, which is correct for a card that hits all enemies
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

    // The same applies to self-inflicted HP loss, which {FormulaHpLoss} shows. It shows red, not green,
    // so the player can tell the cost from the payoff on a card that previews both
    protected virtual int? FormulaHpLossPreview => null;

    private int _fermentTurns;

    protected virtual bool IsFermentCard => false;

    internal bool IsFermentInline => IsFermentCard;

    // Internal so the static calc lambdas can read it off the card arg (no instance capture)
    internal int FermentTurns => _fermentTurns;

    protected virtual string FermentTotalText => "";

    protected virtual bool IsSeepCard => false;

    protected virtual Task OnSeep(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    // Flash the Seep card so the player sees which card triggered. Some Seep effects already show a card,
    // for example a token that previews itself. Those cards opt out to prevent a double flash
    protected virtual bool SeepPreviewsSelf => true;

    // Use VeryEarly, not the plain BeforeSideTurnEnd hook. RegenPower heals and decrements in
    // BeforeSideTurnEndEarly, which runs between the two. From the later hook, a Seep that grants Regen
    // misses the heal for this turn
    public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // Ferment and Seep fire only while the card stays in hand at the owner's turn end
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

    // The card keeps its Ferment potency when you play it, so combat start is the only reset. Deck cards
    // are the same instances in each combat. Every one of them uses this hook, so this covers all piles
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
        // FormulaDamagePreview reads Owner, which throws on a canonical model, for example in the card
        // library. Only a mutable combat instance has an Owner, and the live preview is useful only there
        description.Add("FormulaDamage",
            IsMutable && FormulaDamagePreview is { } d ? $" ([green]{d}[/green])" : "");
        description.Add("FormulaHpLoss",
            IsMutable && FormulaHpLossPreview is { } hp ? $" ([red]{hp}[/red])" : "");
    }
}
