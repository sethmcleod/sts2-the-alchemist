using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Homeostasis : AlchemistCard
{
    private const double Threshold = 0.50;

    public Homeostasis() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }

    protected override bool ConditionalGlow =>
        Owner?.Creature is { } c && c.MaxHp > 0 && (double)c.CurrentHp / c.MaxHp < Threshold;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<HomeostasisPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
