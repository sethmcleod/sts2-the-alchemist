using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Reflux : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Reflux() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("poison", 2, 1); // 2 (3) Poison
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<RefluxPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].BaseValue, Owner.Creature, this);
    }
}
