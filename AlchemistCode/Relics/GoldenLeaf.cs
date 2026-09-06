using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Relics;

public class GoldenLeaf : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.CompoundMix, AlchemistTips.MixHeader };

    // Same window as the flask's Mix: turn 1 after the draw, so the compound lands in a full hand
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;
        if (Owner.Creature.CombatState is not { } combat) return;
        Flash();
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var first = Mixing.Create(combat, Owner, rng.NextItem(Mixing.All));
        var second = Mixing.Create(combat, Owner, rng.NextItem(Mixing.All));
        await Mixing.CreateCompound(choiceContext, Owner, first, second, this);
    }
}
