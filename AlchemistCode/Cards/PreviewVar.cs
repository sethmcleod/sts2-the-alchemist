using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards;

public abstract partial class PreviewVar : DynamicVar
{
    protected PreviewVar(string name, int baseValue) : base(name, baseValue)
    {
    }

    protected abstract int Bonus(AlchemistCard card, Creature? target);

    public int Total(AlchemistCard card, Creature? target) => IntValue + Bonus(card, target);

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        if (card is AlchemistCard { IsMutable: true, CombatState: not null } live)
            PreviewValue = EnchantedValue + Bonus(live, target);
    }
}
