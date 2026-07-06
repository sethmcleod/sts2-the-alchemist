using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Bestow : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Bestow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target?.Player is not { } targetPlayer) return;
        Infusion.InfuseRandomFromHand(targetPlayer, 1); // Infuse a random card in the chosen ally's Hand
        await CardPileCmd.Draw(choiceContext, 1, Owner); // then draw 1 for yourself
    }
}
