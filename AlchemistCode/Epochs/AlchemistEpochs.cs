using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Common;
using Alchemist.AlchemistCode.Cards.Rare;
using Alchemist.AlchemistCode.Cards.Uncommon;
using Alchemist.AlchemistCode.Potions;
using Alchemist.AlchemistCode.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

// The Alchemist's 7 Timeline chapters. Era/EraPosition are assigned dynamically by EpochRegistration
// (scanning free cells at runtime) so we never collide with base or other mods. Triggers: Patches/EpochPatches.cs.

/// <summary>Ch1 "The Unreturned" — obtained by playing a run. Reveals the timeline (Epochs 2-7).</summary>
public class Alchemist1Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST1_EPOCH";

    public override EpochModel[] GetTimelineExpansion() => new[]
    {
        Get<Alchemist2Epoch>(), Get<Alchemist3Epoch>(), Get<Alchemist4Epoch>(),
        Get<Alchemist5Epoch>(), Get<Alchemist6Epoch>(), Get<Alchemist7Epoch>(),
    };

    // Reveal only — the character is already unlocked by installing the mod, so (unlike a base
    // character's Epoch 1) we skip QueueCharacterUnlock and only expand the timeline.
    public override void QueueUnlocks() => QueueTimelineExpansion(GetTimelineExpansion());
}

/// <summary>Ch2 "Field Notes" — beat Act 1. Unlocks the 3 potions.</summary>
public class Alchemist2Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST2_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Potions;
    protected override List<PotionModel> Potions => new()
        { ModelDb.Potion<MarshTonic>(), ModelDb.Potion<RefinedExtract>(), ModelDb.Potion<GoldLeaf>() };
}

/// <summary>Ch3 "The First Distillation" — beat Act 2. Unlocks a poison card trio.</summary>
public class Alchemist3Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST3_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Cards;
    protected override List<CardModel> Cards => new()
        { ModelDb.Card<Tinge>(), ModelDb.Card<Corrode>(), ModelDb.Card<Sepsis>() };
}

/// <summary>Ch4 "Relics of the Expedition" — beat Act 3. Unlocks relics (1 common, 2 uncommon).</summary>
public class Alchemist4Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST4_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Relics;
    protected override List<RelicModel> Relics => new()
        { ModelDb.Relic<SnakeTail>(), ModelDb.Relic<ToxicShard>(), ModelDb.Relic<FluxStone>() };
}

/// <summary>Ch5 "Convalescence" — kill 15 Elites. Unlocks a regen/heal card trio.</summary>
public class Alchemist5Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST5_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Cards;
    protected override List<CardModel> Cards => new()
        { ModelDb.Card<Poultice>(), ModelDb.Card<Hormesis>(), ModelDb.Card<Azoth>() };
}

/// <summary>Ch6 "The Deeper Work" — kill 15 Bosses. Unlocks the 3 rare relics.</summary>
public class Alchemist6Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST6_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Relics;
    protected override List<RelicModel> Relics => new()
        { ModelDb.Relic<AquaVitae>(), ModelDb.Relic<AuricSeal>(), ModelDb.Relic<MidasFruit>() };
}

/// <summary>Ch7 "Rubedo" — beat Ascension 1. Unlocks a transmutation/gold card trio.</summary>
public class Alchemist7Epoch : AlchemistEpoch
{
    public override string Id => "ALCHEMIST-ALCHEMIST7_EPOCH";
    public override EpochUnlockKind UnlockKind => EpochUnlockKind.Cards;
    protected override List<CardModel> Cards => new()
        { ModelDb.Card<Reconstitute>(), ModelDb.Card<Chrysopoeia>(), ModelDb.Card<Libation>() };
}
