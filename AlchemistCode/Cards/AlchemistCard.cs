using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
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

    // ─────────────────────────────────────────────────────────────────────────────
    // Gambit keyword — the card gains an extra effect while the owner is at or
    // below 50% HP. Gambit cards override IsGambitCard (drives the gold glow)
    // and branch on IsReduced in OnPlay. See [[AlchemistKeywords]].
    // ─────────────────────────────────────────────────────────────────────────────
    // internal (not protected) so the static WithCalculatedDamage multiplier lambdas can read it
    // off the card argument — the game requires those calc delegates to capture no instance state.
    internal bool IsReduced => Owner?.Creature is { } c && c.CurrentHp * 2 <= c.MaxHp;

    /// <summary>Override to true on cards with the Gambit keyword (enables the "active" gold glow).</summary>
    protected virtual bool IsGambitCard => false;

    protected override bool ShouldGlowGoldInternal => IsGambitCard && IsReduced;

    // ─────────────────────────────────────────────────────────────────────────────
    // Enchantment bonus damage — cards whose damage is a runtime formula (e.g. Haemorrhage's
    // "double your Regen") deal raw damage via DamageCmd.Attack(decimal), which never creates a
    // DamageVar. The base game applies damage enchantments (e.g. Sharp's +2) only through
    // DamageVar.UpdateCardPreview, so on these cards the bonus neither applies nor shows. Such cards
    // override HasFormulaDamage, add EnchantDamageBonus into their computed damage, and reference
    // {EnchantBonus} in their loc to render the green " + N" suffix. Only the additive part (Sharp,
    // et al.) is handled; multiplicative enchantments on formula-damage cards are not surfaced.
    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>Override to true on cards whose attack damage is computed at play time (no Damage var).</summary>
    protected virtual bool HasFormulaDamage => false;

    /// <summary>Flat bonus damage this card's enchantment adds (0 if unenchanted or a non-damage enchantment).</summary>
    internal int EnchantDamageBonus =>
        Enchantment == null ? 0 : (int)Enchantment.EnchantDamageAdditive(0m, ValueProp.Move);

    // ─────────────────────────────────────────────────────────────────────────────
    // Ferment keyword — the card accrues one "fermented turn" for each of the owner's
    // turns it is held in hand, and its effect scales with that count. Counting happens
    // silently at end of turn (NOT via HasTurnEndInHandEffect, which would play+discard
    // the card). Ferment cards override IsFermentCard and read the count via
    // ConsumeFermentTurns() when they resolve.
    // ─────────────────────────────────────────────────────────────────────────────
    private int _fermentTurns;

    /// <summary>Override to true on cards with the Ferment keyword.</summary>
    protected virtual bool IsFermentCard => false;

    /// <summary>Ferment cards render their Retain keyword inline with "Ferment." — see FermentInlineRetainPatch.</summary>
    internal bool IsFermentInline => IsFermentCard;

    /// <summary>Fermented turns accrued so far (peek without resetting). Internal so the static
    /// WithCalculated* lambdas can read it off the card argument for live green scaling.</summary>
    internal int FermentTurns => _fermentTurns;

    /// <summary>Flat Poison/Regen Ferment cards override this to append a live "(Applies N.)"
    /// parenthetical (shown via {FermentTotal}) so the true fermented total is always explicit.
    /// Return "" when not fermented. Damage/Block Ferment cards leave this empty (their number is
    /// already the live green total via the calculated var).</summary>
    protected virtual string FermentTotalText => "";

    /// <summary>Override to true on cards with the Seep keyword.</summary>
    protected virtual bool IsSeepCard => false;

    /// <summary>The Seep effect: runs at your turn end while this card is still in your hand (unplayed).</summary>
    protected virtual Task OnSeep(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // Both Ferment and Seep fire only while the card is held in hand at the owner's turn end.
        if (Owner == null || !participants.Contains(Owner.Creature)
            || !PileType.Hand.GetPile(Owner).Cards.Contains(this))
            return;
        if (IsFermentCard) _fermentTurns++;
        if (IsSeepCard) await OnSeep(choiceContext);
    }

    /// <summary>Returns the fermented-turn count and resets it — call when the effect resolves.</summary>
    protected int ConsumeFermentTurns()
    {
        var turns = _fermentTurns;
        _fermentTurns = 0;
        return turns;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // Ferment cards show the accrued turn count next to the keyword, e.g. "Ferment (2).".
        // Reference {FermentSuffix} inside the [gold]Ferment…[/gold] tag in the card's loc.
        if (IsFermentCard)
        {
            description.Add("FermentSuffix", _fermentTurns > 0 ? $" ({_fermentTurns})" : "");
            description.Add("FermentTotal", FermentTotalText);
        }
        // Formula-damage cards surface their enchantment's flat bonus as a green " + N" suffix,
        // since the DamageVar enchant preview never reaches their computed damage. Place
        // {EnchantBonus} right after the damage clause in the card's loc.
        if (HasFormulaDamage)
        {
            var bonus = EnchantDamageBonus;
            description.Add("EnchantBonus", bonus > 0 ? $" [green]+ {bonus}[/green]" : "");
        }
    }
}
