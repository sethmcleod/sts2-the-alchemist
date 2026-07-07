using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class MercurialForm : AlchemistCard
{
    public MercurialForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var power = (MercurialFormPower)ModelDb.Power<MercurialFormPower>().ToMutable();
        power.GrantsStrength = IsUpgraded; // upgraded: also gain 1 Strength at the start of your turn
        await PowerCmd.Apply(choiceContext, power, Owner.Creature, 1, Owner.Creature, this);
    }
}
