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

    // The plain hover tip (card faces, the compendium, every FromPower tip) formats Description with
    // only Amount added, so a CanonicalVars entry in the string fails the whole format and the tip
    // prints the raw placeholders. Adding the vars here gives that path the canonical values
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