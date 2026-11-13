using System;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Alchemist.AlchemistCode.Steam;

internal static class ModalSlot
{
    private static readonly TimeSpan Retry = TimeSpan.FromSeconds(1);

    public static async Task<NModalContainer> WaitUntilFree(Func<bool>? gameReady = null,
        CancellationToken cancel = default)
    {
        while (true)
        {
            if (LocManager.Instance != null && (gameReady?.Invoke() ?? true)
                && NModalContainer.Instance is { OpenModal: null } modals)
                return modals;
            await Task.Delay(Retry, cancel);
        }
    }
}
