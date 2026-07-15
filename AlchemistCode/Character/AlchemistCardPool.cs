using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Character;

public class AlchemistCardPool : CustomCardPoolModel
{
    public override string Title => Alchemist.CharacterId; // Not a display name

    // Cards unlocked by later epochs stay out of the pool until that epoch is revealed on the Timeline
    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards) =>
        cards.Where(c => EpochGating.CardUnlocked(c.Id, unlockState));

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();


    // HSV recolors the red base frame; H is a hue rotation, not an absolute color.
    // (Base refs: red 0.025, orange 0.12, green 0.32, blue 0.55, violet 0.85, pink 0.965.)
    public override float H => 0.75f;
    public override float S => 0.4f;
    public override float V => 0.8f;

    public override Color DeckEntryCardColor => new("ffffff");

    public override bool IsColorless => false;
}