using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class MercurialFormPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != Owner.Player) return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
    }
}
