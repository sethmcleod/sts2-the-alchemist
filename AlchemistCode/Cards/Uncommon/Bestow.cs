using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Bestow : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Bestow() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithVar("Infuse", 1, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target?.Player is not { } targetPlayer) return;
        Infusion.InfuseRandomFromHand(targetPlayer, DynamicVars["Infuse"].IntValue);
        await CardPileCmd.Draw(choiceContext, 1, targetPlayer);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
