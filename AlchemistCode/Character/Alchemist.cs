using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Relics;

namespace Alchemist.AlchemistCode.Character;
public class Alchemist : PlaceholderCharacterModel
{
    public const string CharacterId = "Alchemist";

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

    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_alchemist.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_alchemist_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectTransitionPath => $"{MainFile.ResPath}/materials/transitions/alchemist_transition_mat.tres";
    public override string CustomEnergyCounterPath => $"{MainFile.ResPath}/scenes/combat/energy_counters/alchemist_energy_counter.tscn";
}