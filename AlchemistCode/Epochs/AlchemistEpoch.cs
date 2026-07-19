using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

public enum EpochUnlockKind { None, Cards, Relics, Potions }

public abstract class AlchemistEpoch : EpochModel
{
    // The base game looks up the story via Slugify(StoryId) == AlchemistStory.Id
    public override string StoryId => "Alchemist";

    // The code assigns the placement dynamically, so it does not collide with the epoch cells of the
    // base game or another mod
    public override EpochEra Era => EpochRegistration.SlotFor(GetType()).era;
    public override int EraPosition => EpochRegistration.SlotFor(GetType()).pos;

    public virtual EpochUnlockKind UnlockKind => EpochUnlockKind.None;

    protected virtual List<CardModel> Cards => new();
    protected virtual List<RelicModel> Relics => new();
    protected virtual List<PotionModel> Potions => new();

    // The content that this epoch unlocks. EpochGating reads it and gates it behind the epoch reveal
    public IReadOnlyList<CardModel> GatedCards => Cards;
    public IReadOnlyList<RelicModel> GatedRelics => Relics;
    public IReadOnlyList<PotionModel> GatedPotions => Potions;

    public override string UnlockText => UnlockKind switch
    {
        EpochUnlockKind.Cards => CreateCardUnlockText(Cards),
        EpochUnlockKind.Relics => CreateRelicUnlockText(Relics),
        EpochUnlockKind.Potions => CreatePotionUnlockText(Potions),
        _ => base.UnlockText,
    };

    public override void QueueUnlocks()
    {
        switch (UnlockKind)
        {
            case EpochUnlockKind.Cards: NTimelineScreen.Instance.QueueCardUnlock(Cards); break;
            case EpochUnlockKind.Relics: NTimelineScreen.Instance.QueueRelicUnlock(Relics); break;
            case EpochUnlockKind.Potions: NTimelineScreen.Instance.QueuePotionUnlock(Potions); break;
        }
    }
}
