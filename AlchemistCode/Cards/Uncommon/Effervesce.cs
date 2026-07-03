using Alchemist.AlchemistCode.Cards.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Effervesce : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Effervesce() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithVar("cards", 2, 1); // 2 (3) Distillates
        WithTips(_ => new[] { HoverTipFactory.FromCard<Distillate>() });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target?.Player is not { } targetPlayer) return;
        var count = DynamicVars["cards"].IntValue;
        for (var i = 0; i < count; i++)
        {
            var distillate = CombatState.CreateCard<Distillate>(targetPlayer);
            await CardPileCmd.AddGeneratedCardToCombat(distillate, PileType.Hand, targetPlayer);
        }
    }
}
