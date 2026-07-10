using BaseLib.Abstracts;
using BaseLib.Extensions;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// TemporaryStrengthPower resolves its icon by base-game id, which fails for our custom ids and renders the
// missing-texture placeholder. Implementing ICustomPower wires the mod's own icon (BaseLib reads it there)
public abstract class CustomTemporaryStrengthPower : TemporaryStrengthPower, ICustomPower
{
    public string? CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public string? CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
}
