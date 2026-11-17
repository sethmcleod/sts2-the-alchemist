using Alchemist.AlchemistCode.Cards.Basic;
using Alchemist.AlchemistCode.Extensions;
using Alchemist.AlchemistCode.Powers;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemist.AlchemistCode.Character;

// GenerateAnimator differs between game branches, so its override lives in Compat/AlchemistAnimatorCompat.cs
public partial class Alchemist : PlaceholderCharacterModel
{
    public const string CharacterId = "Alchemist";

    public static readonly Color Color = new("8D5DEF");

    public override Color NameColor => Color;
    public override Color MapDrawingColor => new("4B1F9E");

    public override Color EnergyLabelOutlineColor => new("170B02");

    public override float AttackAnimDelay => 0.35f;
    public override float CastAnimDelay => 0.4f;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 69;
    public override int StartingGold => 99;

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
        ModelDb.Card<Jab>(),
        ModelDb.Card<Dose>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<MurkyFlask>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<AlchemistCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AlchemistRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AlchemistPotionPool>();

    public override NCreatureVisuals? CreateCustomVisuals() => AlchemistVisuals.Create();

    // Not null, which preloads a scene that doesn't exist
    public override string CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/fallback");

    protected override IEnumerable<string> ExtraAssetPaths =>
        [AlchemistVisuals.TexturePath, AlchemistRestSite.TexturePath, MercurialFormVfx.TexturePath];

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
    public override string CustomIconOutlineTexturePath => "character_icon_alchemist_outline.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_alchemist.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_alchemist_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_alchemist.png".CharacterUiPath();
    public override RelicIconData CustomYummyCookie => new(
        "alchemist_cookie.png".BigRelicImagePath(),
        "alchemist_cookie.png".RelicImagePath(),
        "alchemist_cookie_outline.png".RelicImagePath());

    public override string CustomArmPointingTexturePath => "multiplayer_hand_alchemist_point.png".CharacterUiPath();
    public override string CustomArmRockTexturePath => "multiplayer_hand_alchemist_rock.png".CharacterUiPath();
    public override string CustomArmPaperTexturePath => "multiplayer_hand_alchemist_paper.png".CharacterUiPath();
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_alchemist_scissors.png".CharacterUiPath();
    public override string CustomCharacterSelectBg => $"{MainFile.ResPath}/scenes/screens/char_select/char_select_bg_alchemist.tscn";
    public override string CustomCharacterSelectTransitionPath => $"{MainFile.ResPath}/materials/transitions/alchemist_transition_mat.tres";
    public override string CustomEnergyCounterPath => $"{MainFile.ResPath}/scenes/combat/energy_counters/alchemist_energy_counter.tscn";

    public override List<string> GetArchitectAttackVfx() => new()
    {
        "vfx/vfx_dramatic_stab",
        "vfx/vfx_slime_impact",
        "vfx/vfx_fire_burst",
        "vfx/vfx_sandy_impact",
        "vfx/vfx_heavy_blunt",
    };

    public override string CustomRestSiteAnimPath => AlchemistRestSite.ScenePath;
    public override string CustomMerchantAnimPath => AlchemistMerchant.ScenePath;

    // Override each sfx, not PlaceholderID, which also swaps the visuals, rest site, merchant and hands
    public override string CharacterSelectSfx => $"{MainFile.ResPath}/audio/alchemist_select.wav";
    public override string CustomAttackSfx => "event:/sfx/characters/osty/osty_attack";
    public override string CustomCastSfx => "event:/sfx/characters/necrobinder/necrobinder_summon";
    public override string CustomDeathSfx => "event:/sfx/characters/osty/osty_die";

    // Not BaseLib's SetupCustomAnimationStates: only this override is handed the Creature
    public override CreatureAnimator GenerateAnimator(MegaSprite controller, Creature creature)
    {
        AlchemistVisuals.StartIdleFlourishes(controller);

        var idle = new AnimState(AlchemistVisuals.IdleAnimation, isLooping: true);
        var lowIdle = AlchemistVisuals.NearDeathAnimation is { } low
            ? new AnimState(low, isLooping: true)
            : idle;

        bool Low() => IsLowHealth(creature);

        AnimState Once(string? name)
        {
            if (name == null) return idle;

            var state = new AnimState(name);
            state.AddNextState(idle, () => !Low());
            state.AddNextState(lowIdle, Low);
            return state;
        }

        var hurt = Once(AlchemistVisuals.HurtAnimation);
        var attack = Once(AlchemistVisuals.AttackAnimation);
        var cast = Once(AlchemistVisuals.CastAnimation);
        var heavy = Once(AlchemistVisuals.HeavyAttackAnimation ?? AlchemistVisuals.AttackAnimation);
        var dead = AlchemistVisuals.DeathAnimation is { } death ? new AnimState(death) : idle;

        var relaxed = idle;
        if (AlchemistVisuals.RelaxedAnimation is { } relaxedName)
        {
            relaxed = new AnimState(relaxedName, isLooping: true);
            relaxed.AddBranch("Idle", idle);
        }

        var animator = new CreatureAnimator(Low() ? lowIdle : idle, controller);
        animator.AddAnyState("Idle", idle, () => !Low());
        animator.AddAnyState("Idle", lowIdle, Low);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("heavyAttack", heavy);
        animator.AddAnyState("PowerUp", cast);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }
}
