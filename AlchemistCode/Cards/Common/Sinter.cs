using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Sinter : AlchemistCard
{
    private const int ExhaustThreshold = 7;

    public Sinter() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(7, 3);
    }

    private bool ExhaustReady => Owner != null && PileType.Exhaust.GetPile(Owner).Cards.Count >= ExhaustThreshold;

    protected override bool ConditionalGlow => ExhaustReady;

    // A card is one of its own hook listeners, so it can zero out its own cost while the condition holds
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
