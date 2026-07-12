using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Suffuse : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Suffuse() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }

    // Glows while your HP is below SuffusePower's trigger threshold
    protected override bool ConditionalGlow =>
        Owner?.Creature is { } c && c.MaxHp > 0 && (double)c.CurrentHp / c.MaxHp < 0.50;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SuffusePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
