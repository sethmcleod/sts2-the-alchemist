
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class WindfallPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (potion.Owner == Owner.Player && CombatState != null)
        {
            Flash();
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, Owner.Player);
            await PlayerCmd.GainEnergy(1, Owner.Player);
        }
    }
}
