using System.Globalization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards;

// A card number that is a base value plus a live bonus read from the combat. The preview carries
// the total, so `{Var:diff()}` prints it in green the way the game prints Strength-modified damage,
// and Total gives the play path the same sum, so the number shown and the number dealt cannot
// drift apart. The base value stays the plain number, which is what the upgrade preview and the
// compendium show.
//
// SmartFormat reads a var two ways: `plural` through IConvertible, and a bare `{Var}` or `choose`
// through ToString. The base class feeds both BaseValue. This feeds both the preview, as the game's
// CalculatedVar does, so a plural never disagrees with the number beside it. IntValue still reads
// BaseValue for game logic
public abstract class PreviewVar : DynamicVar
{
    protected PreviewVar(string name, int baseValue) : base(name, baseValue)
    {
    }

    // The live part of the number for this card against this target, or 0 when nothing applies
    protected abstract int Bonus(AlchemistCard card, Creature? target);

    public int Total(AlchemistCard card, Creature? target) => IntValue + Bonus(card, target);

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        // Only a live card in a combat has a bonus. Between combats the stored state is stale until
        // the next combat resets it, and a canonical model has no Owner to read
        if (card is AlchemistCard { IsMutable: true, CombatState: not null } live)
            PreviewValue = EnchantedValue + Bonus(live, target);
    }

    protected override decimal GetBaseValueForIConvertible() => PreviewValue;

    public override string ToString() => ((int)PreviewValue).ToString(CultureInfo.InvariantCulture);
}
