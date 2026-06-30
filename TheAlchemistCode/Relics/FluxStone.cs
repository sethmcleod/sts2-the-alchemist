using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheAlchemist.TheAlchemistCode.Relics;

public class FluxStone : TheAlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != Owner) return;
        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, Owner);
    }
}
