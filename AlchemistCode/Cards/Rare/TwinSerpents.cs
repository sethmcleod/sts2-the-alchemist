using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class TwinSerpents : AlchemistCard
{
    public TwinSerpents() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        // WithEnergy, not WithVar: the {Energy:energyIcons()} formatter rejects a plain DynamicVar
        WithEnergy(1, 0);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    protected override bool ConditionalGlow =>
        Owner?.Creature is { } c && c.HasPower<PoisonPower>() && c.HasPower<AntitoxinPower>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<TwinSerpentsPower>(choiceContext, Owner.Creature,
            DynamicVars["Energy"].IntValue, Owner.Creature, this);
    }
}
