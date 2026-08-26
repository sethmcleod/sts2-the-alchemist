using System.Collections.Generic;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Relics;

// The Mix lane's relic: the shelf is stocked before every fight, your pick, and it keeps.
// The picker runs on turn 1 rather than BeforeCombatStart, because only the turn-start hook
// carries a real choice context (base ChoicesParadox does the same)
public class EverflowingChalice : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [.. Mixing.MixTips(upgraded: true), HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;
        Flash();
        var mix = await Mixing.Choose(choiceContext, Owner, upgraded: true);
        if (mix == null) return;
        CardCmd.ApplyKeyword(mix, CardKeyword.Retain);
        await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
    }
}
