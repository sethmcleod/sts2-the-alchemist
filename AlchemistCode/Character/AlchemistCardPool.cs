using BaseLib.Abstracts;
using Alchemist.AlchemistCode.Extensions;
using Godot;

namespace Alchemist.AlchemistCode.Character;

public class AlchemistCardPool : CustomCardPoolModel
{
    public override string Title => Alchemist.CharacterId; //This is not a display name.

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();


    /* These HSV values recolor the shared base card frame via res://shaders/hsv.gdshader.
    BaseLib auto-generates a ShaderMaterial from them (no image copying needed) because
    CardFrameMaterialPath is left at its default "card_frame_red".
    H is a hue ROTATION of the red base art, not an absolute color, so expect to experiment.
    Base game reference points: red H=0.025, orange 0.12, green 0.32, blue 0.55,
    curse(violet) 0.85, pink 0.965. Purple (#5D3FD3) sits ~0.70-0.80. */
    public override float H => 0.75f; //Hue; changes the color.
    public override float S => 0.4f; //Saturation (0-5; 1 = unchanged)
    public override float V => 0.8f; //Brightness (1 = unchanged); lower = darker frame

    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load Alchemist/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("ffffff");

    public override bool IsColorless => false;
}