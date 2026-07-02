using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Resolve : AlchemistCard
{
    public Resolve() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var power = await PowerCmd.Apply<ResolvePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        // Upgrade lowers the HP-per-Strength threshold (20 -> 15), yielding more Strength.
        power?.SetThreshold(IsUpgraded ? 15 : 20);
    }
}
