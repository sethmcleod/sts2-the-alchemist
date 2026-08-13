using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Extensions;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemist.AlchemistCode.Character;

public class Alchemist : PlaceholderCharacterModel
{
    public const string CharacterId = "Alchemist";

    // Medium violet, matches DeckEntryCardColor; lighter than iris for
    // readability on dark backgrounds, bluer than the Necrobinder's EE82EE
    public static readonly Color Color = new("8D5DEF");

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
        ModelDb.Card<Spike>(),
        ModelDb.Card<Antidote>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<WeatheredKit>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<AlchemistCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AlchemistRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AlchemistPotionPool>();

    // The combat model is the true art. The other placeholder assets stay on the ironclad
    public override NCreatureVisuals? CreateCustomVisuals() => AlchemistVisuals.Create();

    // AssetPaths preloads the visuals path with the other character assets, but CreateCustomVisuals
    // always wins over it, thus the ironclad combat scene here would load 4 atlas pages that nothing
    // shows. The base game fallback scene is one small sprite, and it is a complete NCreatureVisuals,
    // thus it still catches a failed model load. Null is not an option here: it makes the base getter
    // ask for a creature_visuals scene named after the character, which does not exist, and the
    // preload then fails twice with an error each time
    public override string CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/fallback");

    // The atlas page of the model loads with the other character assets, not on the first combat
    protected override IEnumerable<string> ExtraAssetPaths =>
        [AlchemistVisuals.TexturePath, AlchemistRestSite.TexturePath];

    // Attack, cast and death are still missing from the skeleton, and an omitted name falls back
    // to the idle. The blink rides a second track with its own clock, thus it never locks to the
    // loop of the idle
    public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller)
    {
        AlchemistVisuals.StartBlinking(controller);
        return SetupAnimationState(controller, AlchemistVisuals.IdleAnimation,
            hitName: AlchemistVisuals.HurtAnimation);
    }

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

    public override string CustomArmPointingTexturePath => "multiplayer_hand_alchemist_point.png".CharacterUiPath();
    public override string CustomArmRockTexturePath => "multiplayer_hand_alchemist_rock.png".CharacterUiPath();
    public override string CustomArmPaperTexturePath => "multiplayer_hand_alchemist_paper.png".CharacterUiPath();
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_alchemist_scissors.png".CharacterUiPath();
    public override string CustomCharacterSelectBg => $"{MainFile.ResPath}/scenes/screens/char_select/char_select_bg_alchemist.tscn";
    public override string CustomCharacterSelectTransitionPath => $"{MainFile.ResPath}/materials/transitions/alchemist_transition_mat.tres";
    public override string CustomEnergyCounterPath => $"{MainFile.ResPath}/scenes/combat/energy_counters/alchemist_energy_counter.tscn";

    // Both scenes hold no Spine node of their own. SpineScenePatch puts the model in after BaseLib
    // turns each one into its game type. The shop reuses the combat skeleton, the same way every
    // base game character does, and plays its relaxed_loop
    public override string CustomRestSiteAnimPath => AlchemistRestSite.ScenePath;
    public override string CustomMerchantAnimPath => AlchemistMerchant.ScenePath;

    // Borrowed base-game sfx. Override each one rather than PlaceholderID, which also controls the creature
    // visuals, the rest site and merchant animations, and the multiplayer hands. A res:// path plays through
    // Godot audio instead of FMOD, routed by BaseLib's PlayResourcePatch. scripts/gen_select_sfx.py makes the wav
    public override string CharacterSelectSfx => $"{MainFile.ResPath}/audio/alchemist_select.wav";
    public override string CustomAttackSfx => "event:/sfx/characters/silent/silent_attack";
    public override string CustomCastSfx => "event:/sfx/characters/necrobinder/necrobinder_cast";
    public override string CustomDeathSfx => "event:/sfx/characters/silent/silent_die";
}