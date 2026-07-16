using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Extensions;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Character;

public class Alchemist : PlaceholderCharacterModel
{
    public const string CharacterId = "Alchemist";

    public static readonly Color Color = new("5D3FD3");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 69;
    public override int StartingGold => 75;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<Nigredo>(),
        ModelDb.Card<Prime>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<WeatheredKit>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<AlchemistCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AlchemistRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AlchemistPotionPool>();

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_alchemist.png".CharacterUiPath();
    // Without this override the outline silhouette falls back to a non-existent base path
    public override string CustomIconOutlineTexturePath => "character_icon_alchemist_outline.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_alchemist.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_alchemist_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_alchemist.png".CharacterUiPath();
    public override string CustomCharacterSelectTransitionPath => $"{MainFile.ResPath}/materials/transitions/alchemist_transition_mat.tres";
    public override string CustomEnergyCounterPath => $"{MainFile.ResPath}/scenes/combat/energy_counters/alchemist_energy_counter.tscn";

    // Borrowed until the Alchemist has its own FMOD bank. PlaceholderCharacterModel derives these
    // from PlaceholderID ("ironclad"), so without them every card sounds like a greatsword swing.
    // Override the sfx individually rather than PlaceholderID, which also drives the creature
    // visuals, rest site and merchant anims, and multiplayer hands
    public override string CustomAttackSfx => "event:/sfx/characters/silent/silent_attack";
    public override string CustomCastSfx => "event:/sfx/characters/necrobinder/necrobinder_cast";
    public override string CustomDeathSfx => "event:/sfx/characters/silent/silent_die";
}