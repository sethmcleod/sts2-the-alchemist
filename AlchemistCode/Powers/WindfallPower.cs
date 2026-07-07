using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class WindfallPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Copies", 0m) };

    public void RegisterCopy() => DynamicVars["Copies"].BaseValue += 1;

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (potion.Owner == Owner.Player && CombatState != null)
        {
            Flash();
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, Owner.Player);
            var energy = DynamicVars["Copies"].IntValue;
            if (energy > 0)
                await PlayerCmd.GainEnergy(energy, Owner.Player);
        }
    }
}
