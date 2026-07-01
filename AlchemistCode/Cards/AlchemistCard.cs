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

namespace Alchemist.AlchemistCode.Cards;

[Pool(typeof(AlchemistCardPool))]
public abstract class AlchemistCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // ─────────────────────────────────────────────────────────────────────────────
    // Mettle keyword — the card gains an extra effect while the owner is at or
    // below 50% HP. Mettle cards override IsMettleCard (drives the gold glow)
    // and branch on IsReduced in OnPlay. See [[AlchemistKeywords]].
    // ─────────────────────────────────────────────────────────────────────────────
    // internal (not protected) so the static WithCalculatedDamage multiplier lambdas can read it
    // off the card argument — the game requires those calc delegates to capture no instance state.
    internal bool IsReduced => Owner?.Creature is { } c && c.CurrentHp * 2 <= c.MaxHp;

    /// <summary>Override to true on cards with the Mettle keyword (enables the "active" gold glow).</summary>
    protected virtual bool IsMettleCard => false;

    protected override bool ShouldGlowGoldInternal => IsMettleCard && IsReduced;

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

    /// <summary>Fermented turns accrued so far (peek without resetting).</summary>
    protected int FermentTurns => _fermentTurns;

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (IsFermentCard && Owner != null
            && participants.Contains(Owner.Creature)
            && PileType.Hand.GetPile(Owner).Cards.Contains(this))
            _fermentTurns++;
        return Task.CompletedTask;
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
            description.Add("FermentSuffix", _fermentTurns > 0 ? $" ({_fermentTurns})" : "");
    }
}
