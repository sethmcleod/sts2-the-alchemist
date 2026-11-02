using Alchemist.AlchemistCode.Potions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.None)]
public class Bestow : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Bestow() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target?.Player is not { } targetPlayer) return;
        await Brewing.Produce(targetPlayer, Owner.RunState.Rng.CombatPotionGeneration);
    }
}
