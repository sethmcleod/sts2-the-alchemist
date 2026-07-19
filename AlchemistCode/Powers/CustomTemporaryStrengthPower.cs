using BaseLib.Abstracts;
using BaseLib.Extensions;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// TemporaryStrengthPower finds its icon by base-game id. This fails for our custom ids, and the game
// shows the placeholder texture. ICustomPower supplies the mod icon, because BaseLib reads it there
public abstract class CustomTemporaryStrengthPower : TemporaryStrengthPower, ICustomPower
{
    public string? CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public string? CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
}
