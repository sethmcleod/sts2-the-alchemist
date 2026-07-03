using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Panacea : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Panacea() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("amount", 3, 1); // 3 (4) Regen + heal
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PanaceaPower>(choiceContext, Owner.Creature,
            DynamicVars["amount"].BaseValue, Owner.Creature, this);
    }
}
