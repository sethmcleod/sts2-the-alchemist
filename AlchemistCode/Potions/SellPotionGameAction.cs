using Alchemist.AlchemistCode.Patches;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.TestSupport;

namespace Alchemist.AlchemistCode.Potions;

public sealed class SellPotionGameAction : GameAction
{
    private readonly Player _player;
    private readonly uint _potionSlotIndex;

    public SellPotionGameAction(Player player, uint potionSlotIndex)
    {
        _player = player;
        _potionSlotIndex = potionSlotIndex;
    }

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => GameActionType.NonCombat;

    protected override async Task ExecuteAction()
    {
        var potion = _player.GetPotionAtSlotIndex((int)_potionSlotIndex);
        if (potion == null || !PotionSellPatches.IsSellable(potion))
        {
            Cancel();
            return;
        }
        await PotionSellPatches.CompleteSale(potion);
    }

    protected override void CancelAction()
    {
        var potion = _player.GetPotionAtSlotIndex((int)_potionSlotIndex);
        if (TestMode.IsOff && NRun.Instance != null && LocalContext.IsMe(_player) && potion != null)
            NRun.Instance.GlobalUi.TopBar.PotionContainer.OnPotionUseOrDiscardCanceled(potion);
        potion?.AfterUsageCanceled();
    }

    public override INetAction ToNetAction() => new NetSellPotionGameAction { potionSlotIndex = _potionSlotIndex };
}

public struct NetSellPotionGameAction : INetAction
{
    public uint potionSlotIndex;

    public GameAction ToGameAction(Player player) => new SellPotionGameAction(player, potionSlotIndex);

    public void Serialize(PacketWriter writer) => writer.WriteUInt(potionSlotIndex, 4);

    public void Deserialize(PacketReader reader) => potionSlotIndex = reader.ReadUInt(4);

    public override string ToString() => $"{nameof(NetSellPotionGameAction)} slot {potionSlotIndex}";
}
