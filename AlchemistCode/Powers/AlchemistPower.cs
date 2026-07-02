using BaseLib.Abstracts;
using BaseLib.Extensions;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Alchemist.AlchemistCode.Powers;

/// <summary>
/// Base class for all Alchemist powers. Icons load by convention from
/// Alchemist/images/powers/&lt;power_id&gt;.png (64x64) and powers/big/&lt;power_id&gt;.png (256x256).
/// </summary>
public abstract class AlchemistPower : CustomPowerModel
{
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    public abstract override PowerType Type { get; }
    public abstract override PowerStackType StackType { get; }
}