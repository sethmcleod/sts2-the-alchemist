using BaseLib.Abstracts;
using TheAlchemist.TheAlchemistCode.Extensions;
using Godot;

namespace TheAlchemist.TheAlchemistCode.Character;

public class TheAlchemistRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => TheAlchemist.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}