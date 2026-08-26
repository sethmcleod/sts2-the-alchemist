using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class ExtraDose : AlchemistRelic
{
    private const int Antitoxin = 1;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[]
        {
            HoverTipFactory.FromPower<PoisonPower>(),
            HoverTipFactory.FromPower<AntitoxinPower>(),
        };

    // Granting Antitoxin re-enters this hook, and an enemy answering with more Poison can too
    private bool _resolving;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_resolving || amount <= 0m || power is not PoisonPower || power.Owner != Owner.Creature) return;
        Flash();
        _resolving = true;
        try
        {
            await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, Antitoxin,
                Owner.Creature, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
