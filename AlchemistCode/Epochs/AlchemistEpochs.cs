using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Common;
using Alchemist.AlchemistCode.Cards.Rare;
using Alchemist.AlchemistCode.Cards.Uncommon;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

public class Alchemist1Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST1_EPOCH";

    public override EpochModel[] GetTimelineExpansion() => new[]
    {
        Get<Alchemist2Epoch>(), Get<Alchemist3Epoch>(), Get<Alchemist4Epoch>(),
        Get<Alchemist5Epoch>(), Get<Alchemist6Epoch>(), Get<Alchemist7Epoch>(),
    };

    public override void QueueUnlocks() => QueueTimelineExpansion(GetTimelineExpansion());
}

public class Alchemist2Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST2_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Cards;
    protected override List<CardModel> Cards => new()
        { ModelDb.Card<Congeal>(), ModelDb.Card<Corrode>(), ModelDb.Card<SweatItOut>() };
}

public class Alchemist3Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST3_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Potions;
    protected override List<PotionModel> Potions => new()
        { ModelDb.Potion<MarshTonic>(), ModelDb.Potion<RefinedExtract>(), ModelDb.Potion<GoldLeaf>() };
}

public class Alchemist4Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST4_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Relics;
    protected override List<RelicModel> Relics => new()
        { ModelDb.Relic<SnakeTail>(), ModelDb.Relic<ToxicShard>(), ModelDb.Relic<FluxStone>() };
}

public class Alchemist5Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST5_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Cards;
    protected override List<CardModel> Cards => new()
        { ModelDb.Card<Carapace>(), ModelDb.Card<RollingBoil>(), ModelDb.Card<Amalgam>() };
}

public class Alchemist6Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST6_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Cards;
    protected override List<CardModel> Cards => new()
        { ModelDb.Card<FullMeasure>(), ModelDb.Card<Masterwork>(), ModelDb.Card<GoldenTouch>() };
}

public class Alchemist7Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST7_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Relics;
    protected override List<RelicModel> Relics => new()
        { ModelDb.Relic<AquaVitae>(), ModelDb.Relic<AuricSeal>(), ModelDb.Relic<MidasFruit>() };
}
