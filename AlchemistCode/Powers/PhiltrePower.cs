
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class PhiltrePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            // Temporary ("this turn") Strength/Dexterity — removed at end of turn.
            await PowerCmd.Apply<PhiltreStrengthPower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, null);
            await PowerCmd.Apply<PhiltreDexterityPower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, null);
        }
    }
}
