using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards;

// A card number that grows with fermentation. The preview carries the fermented total, so
// `{Var:diff()}` prints it in green the way the base game prints Strength-modified damage, with no
// parenthesis. The base value stays the unfermented number, which is what the upgrade preview and
// the compendium show. The per-turn growth is either a flat number or another var on the same
// card, so an upgrade to the growth rate is read live
public sealed class FermentVar : DynamicVar
{
    private readonly string? _perTurnVar;
    private readonly int _perTurnFlat;

    public FermentVar(string name, int baseValue, int perTurn) : base(name, baseValue)
    {
        _perTurnFlat = perTurn;
    }

    public FermentVar(string name, int baseValue, string perTurnVar) : base(name, baseValue)
    {
        _perTurnVar = perTurnVar;
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        if (card is not AlchemistCard { IsMutable: true } ferment) return;
        var turns = ferment.FermentTurns;
        if (turns <= 0) return;
        var perTurn = _perTurnVar != null ? (int)card.DynamicVars[_perTurnVar].BaseValue : _perTurnFlat;
        PreviewValue = EnchantedValue + perTurn * turns;
    }
}
