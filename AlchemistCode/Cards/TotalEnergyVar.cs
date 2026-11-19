using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards;

public sealed class TotalEnergyVar(string name, int energy, Func<AlchemistCard, int> bonus) : EnergyVar(name, energy)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        if (card is AlchemistCard { IsMutable: true, CombatState: not null } live)
            PreviewValue = EnchantedValue + bonus(live);
    }
}
