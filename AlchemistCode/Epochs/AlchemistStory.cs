using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

/// <summary>
/// Groups the Alchemist's 7 chapters into a Timeline "story". The base game looks up a story by
/// <c>Slugify(epoch.StoryId)</c> == this <see cref="Id"/> — our epochs use StoryId "Alchemist", which
/// slugifies to "ALCHEMIST". Story display name comes from loc key <c>STORY_ALCHEMIST</c> (epochs table).
/// Order here is the chapter navigation order (Prev/Next).
/// </summary>
public sealed class AlchemistStory : StoryModel
{
    /// <summary>The story dictionary key — must equal Slugify(epoch.StoryId) = Slugify("Alchemist").</summary>
    public const string StoryKey = "ALCHEMIST";
    protected override string Id => StoryKey;

    public override EpochModel[] Epochs => new[]
    {
        EpochModel.Get<Alchemist1Epoch>(),
        EpochModel.Get<Alchemist2Epoch>(),
        EpochModel.Get<Alchemist3Epoch>(),
        EpochModel.Get<Alchemist4Epoch>(),
        EpochModel.Get<Alchemist5Epoch>(),
        EpochModel.Get<Alchemist6Epoch>(),
        EpochModel.Get<Alchemist7Epoch>(),
    };
}
