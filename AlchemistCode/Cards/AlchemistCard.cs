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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards;

[Pool(typeof(AlchemistCardPool))]
public abstract class AlchemistCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Internal so the static calc-damage lambdas can read it off the card arg (they must capture no instance state)
    internal bool IsReduced => Owner?.Creature is { } c && c.CurrentHp * 2 <= c.MaxHp;

    // True when this card currently carries an enchantment (was Infused this combat). Safe on canonical models
    internal bool IsEnchanted => Enchantment != null;

    protected virtual bool IsGambitCard => false;

    // Cards with a play-time conditional bonus override this to glow gold while that condition currently
    // holds, the same way Gambit cards glow while you're at low HP
    protected virtual bool ConditionalGlow => false;

    protected override bool ShouldGlowGoldInternal => (IsGambitCard && IsReduced) || ConditionalGlow;

    // True when the owner's current HP sits within [lower, upper] as a fraction of Max HP
    internal bool HpFractionInRange(double lower, double upper)
    {
        if (Owner?.Creature is not { } c || c.MaxHp <= 0) return false;
        var pct = (double)c.CurrentHp / c.MaxHp;
        return pct >= lower && pct <= upper;
    }

    // "Lose N HP" — unblockable, unpowered self-damage
    protected Task LoseHp(PlayerChoiceContext choiceContext, int amount) =>
        CreatureCmd.Damage(choiceContext, Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, this, null);

    // Formula-damage cards deal raw DamageCmd.Attack(decimal) with no DamageVar, and only DamageVar runs an
    // enchantment's damage hooks — so they fold them in by hand, in the same order DamageVar uses
    internal int ApplyEnchantDamage(int damage)
    {
        if (Enchantment is not { } enchantment) return damage;
        decimal value = damage;
        value += enchantment.EnchantDamageAdditive(value, ValueProp.Move);
        value *= enchantment.EnchantDamageMultiplicative(value, ValueProp.Move);
        return (int)value;
    }

    // Cards whose damage is a runtime formula (no DamageVar to preview) return the current computed total so
    // the card face can show it live via {FormulaDamage}. Null when it can't be computed (e.g. the card library)
    protected virtual int? FormulaDamagePreview => null;

    private int _fermentTurns;

    protected virtual bool IsFermentCard => false;

    internal bool IsFermentInline => IsFermentCard;

    // Internal so the static calc lambdas can read it off the card arg (no instance capture)
    internal int FermentTurns => _fermentTurns;

    protected virtual string FermentTotalText => "";

    protected virtual bool IsSeepCard => false;

    protected virtual Task OnSeep(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    // Flash the seeping card so the player sees which one triggered. Cards whose Seep already surfaces a card
    // (e.g. adding a token that previews itself) opt out to avoid a redundant double flash
    protected virtual bool SeepPreviewsSelf => true;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // Ferment and Seep only fire while the card is held in hand at the owner's turn end
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

    // Returns the fermented-turn count and resets it
    protected int ConsumeFermentTurns()
    {
        var turns = _fermentTurns;
        _fermentTurns = 0;
        return turns;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        if (IsFermentCard)
        {
            description.Add("FermentSuffix", _fermentTurns > 0 ? $" ({_fermentTurns})" : "");
            description.Add("FermentTotal", FermentTotalText);
        }
        // FormulaDamagePreview reads Owner, which throws on canonical models (e.g. the compendium/card library).
        // Only mutable combat instances have an Owner, and the live preview is only meaningful there
        description.Add("FormulaDamage",
            IsMutable && FormulaDamagePreview is { } d ? $" ([green]{d}[/green])" : "");
    }
}
