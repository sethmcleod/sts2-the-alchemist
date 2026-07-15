using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Character;

public class AlchemistRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Alchemist.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // Relics unlocked by later epochs stay out of the pool until that epoch is revealed on the Timeline
    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState) =>
        AllRelics.Where(r => EpochGating.RelicUnlocked(r.Id, unlockState));
}