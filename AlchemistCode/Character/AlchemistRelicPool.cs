using BaseLib.Abstracts;
using Alchemist.AlchemistCode.Extensions;
using Godot;

namespace Alchemist.AlchemistCode.Character;

public class AlchemistRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Alchemist.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}