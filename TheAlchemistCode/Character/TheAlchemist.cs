using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using TheAlchemist.TheAlchemistCode.Cards.Basic;
using TheAlchemist.TheAlchemistCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using TheAlchemist.TheAlchemistCode.Relics;

namespace TheAlchemist.TheAlchemistCode.Character;
public class TheAlchemist : PlaceholderCharacterModel
{
    public const string CharacterId = "TheAlchemist";

    public static readonly Color Color = new("5D3FD3");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;

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
        ModelDb.Card<Leaden>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<TarnishedFlask>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<TheAlchemistCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TheAlchemistRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TheAlchemistPotionPool>();
    
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_alchemist.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_alchemist_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectTransitionPath => $"{MainFile.ResPath}/materials/transitions/alchemist_transition_mat.tres";
    public override string CustomEnergyCounterPath => $"{MainFile.ResPath}/scenes/combat/energy_counters/alchemist_energy_counter.tscn";
}