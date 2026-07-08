using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Transmutation : AlchemistCard
{
    public Transmutation() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<TransmutationPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (IsUpgraded)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
