using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Extensions;

namespace Alchemist.AlchemistCode.Relics;

/// <summary>
/// Base class for all Alchemist relics (the [Pool] attribute ties every subclass to this
/// character's pool). Icons load by convention from Alchemist/images/relics/&lt;relic_id&gt;.png
/// (94x94), relics/&lt;relic_id&gt;_outline.png, and relics/big/&lt;relic_id&gt;.png (256x256).
/// </summary>
[Pool(typeof(AlchemistRelicPool))]
public abstract class AlchemistRelic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();

    protected override string PackedIconOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();

    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
}