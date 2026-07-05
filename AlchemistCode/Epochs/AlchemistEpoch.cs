using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

/// <summary>
/// Which kind of content an epoch gates. Drives both the unlock screen (QueueUnlocks) and the
/// GetXUnlockEpochIds patches — those auto-collect our epoch ids by this kind, so there is no
/// separate hardcoded id list to keep in sync (unlike TheSorceress).
/// </summary>
public enum EpochUnlockKind { None, Cards, Relics, Potions }

/// <summary>
/// Shared base for the Alchemist's 7 Timeline chapters. Each concrete epoch sets its <see cref="Era"/>,
/// <see cref="EpochModel.EraPosition"/>, <see cref="UnlockKind"/> and the matching content list; this base
/// wires up the unlock text + unlock screen from that. Epoch ids follow the base-game convention
/// (<c>{CHAR}{N}_EPOCH</c>) prefixed with our loc namespace so the loc keys resolve and the base game's
/// own <c>Character.Id.Entry + "N_EPOCH"</c> derivation would also find them.
/// </summary>
public abstract class AlchemistEpoch : EpochModel
{
    /// <summary>PascalCase story name; base looks up the story via Slugify(StoryId) == AlchemistStory.Id.</summary>
    public override string StoryId => "Alchemist";

    // Timeline placement is assigned DYNAMICALLY (not hardcoded) so we don't collide with base epochs or
    // other mods' epochs (e.g. TheSorceress, which hardcodes Invitation0/5 pos 4 — the cells we used to take).
    // EpochRegistration scans the occupied cells at runtime and hands each of our epochs a free one.
    public override EpochEra Era => EpochRegistration.SlotFor(GetType()).era;
    public override int EraPosition => EpochRegistration.SlotFor(GetType()).pos;

    public virtual EpochUnlockKind UnlockKind => EpochUnlockKind.None;

    // A gating epoch overrides exactly one of these. Left empty for the Ch1 reveal node.
    protected virtual List<CardModel> Cards => new();
    protected virtual List<RelicModel> Relics => new();
    protected virtual List<PotionModel> Potions => new();

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
