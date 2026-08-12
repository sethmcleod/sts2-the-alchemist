using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Sinter : AlchemistCard
{
    private const int ExhaustThreshold = 5;

    public Sinter() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(7, 3);
    }

    // IsMutable, not Owner != null: Owner throws on a canonical model rather than returning null. This
    // guards itself because it also feeds the cost hook, which the base glow gate does not cover
    private bool ExhaustReady => IsMutable && PileType.Exhaust.GetPile(Owner).Cards.Count >= ExhaustThreshold;

    protected override bool ConditionalGlow => ExhaustReady;

    // A card is one of its own hook listeners, so it can discount itself while the condition holds
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || !ExhaustReady) return false;
        modifiedCost = 0m;
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
