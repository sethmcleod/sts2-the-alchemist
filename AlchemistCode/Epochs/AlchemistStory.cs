using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

public sealed class AlchemistStory : StoryModel
{
    // Must equal Slugify(epoch.StoryId) = Slugify("Alchemist")
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
