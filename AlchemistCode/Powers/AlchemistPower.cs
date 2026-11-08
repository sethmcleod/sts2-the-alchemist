using BaseLib.Abstracts;
using BaseLib.Extensions;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Powers;

public abstract class AlchemistPower : CustomPowerModel
{
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    public abstract override PowerType Type { get; }
    public abstract override PowerStackType StackType { get; }

    // Plain hover tips format Description with only Amount, so without these vars they print raw placeholders
    public override LocString Description
    {
        get
        {
            var description = base.Description;
            DynamicVars.AddTo(description);
            return description;
        }
    }
}