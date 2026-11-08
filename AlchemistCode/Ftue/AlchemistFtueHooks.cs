using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;
using AlchemistCharacter = Alchemist.AlchemistCode.Character.Alchemist;

namespace Alchemist.AlchemistCode.Ftue;

// Every client runs these for every player, so each shows tips for its own Alchemist only
public sealed class AlchemistFtueHooks() : CustomSingletonModel(HookType.Combat)
{
    private static bool IsLocalAlchemist(Player? player) =>
        player is { Character: AlchemistCharacter } && LocalContext.IsMe(player);

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (IsLocalAlchemist(player) && player.PlayerCombatState is { TurnNumber: 1 })
            AlchemistFtue.Queue(AlchemistFtue.Antitoxin, player.Creature);
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is PoisonPower or AntitoxinPower
            && IsLocalAlchemist(power.Owner.Player)
            && power.Owner.GetPower<PoisonPower>() is { } poison
            && poison.CalculateTotalDamageNextTurn() > 0)
            AlchemistFtue.Queue(AlchemistFtue.Poison, power.Owner);
        return Task.CompletedTask;
    }
}
