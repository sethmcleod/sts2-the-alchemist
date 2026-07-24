using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Azoth : AlchemistCard
{
    private const int ExhaustThreshold = 5;

    public Azoth() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(10, 4);
        WithEnergy(1, 1);
    }

    protected override bool ConditionalGlow =>
        PileType.Exhaust.GetPile(Owner).Cards.Count >= ExhaustThreshold;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_starry_impact")).Execute(choiceContext);
        if (PileType.Exhaust.GetPile(Owner).Cards.Count >= ExhaustThreshold)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}
