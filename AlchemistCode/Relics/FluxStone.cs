using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Relics;

public class FluxStone : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != Owner) return;
        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, Owner);
    }
}
