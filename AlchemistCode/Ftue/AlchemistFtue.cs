using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alchemist.AlchemistCode.Patches;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace Alchemist.AlchemistCode.Ftue;

// One-time tips in the game's own popup style, pointed at the Antitoxin bar. Every node is a
// base-game type: a Godot virtual on a mod-defined node never runs, so the root is a stock NFtue,
// the button is the base confirm scene, and the wiring is C# signals. Text lives in ftues.json
public static class AlchemistFtue
{
    public sealed record Tip(string Id, string LocKey);

    public static readonly Tip Antitoxin = new("alchemist_antitoxin_ftue", "ALCHEMIST-ANTITOXIN_FTUE");
    public static readonly Tip Poison = new("alchemist_poison_ftue", "ALCHEMIST-POISON_FTUE");

    private const string PopupTexture = "res://images/ftue/ftue_popup.png";
    private const string PopupMaterial = "res://shaders/ftue_popup.tres";
    private const string ArrowTexture = "res://images/ftue/ftue_pointer_arrow.png";
    private const string HeaderFont = "res://themes/kreon_bold_glyph_space_two.tres";
    private const string BodyFont = "res://themes/kreon_regular_glyph_space_one.tres";
    private const string BodyBoldFont = "res://themes/kreon_bold_glyph_space_one.tres";
    private const string ConfirmScene = "ftue/ftue_confirm_button";

    // The base slab is 579x257; a taller slab keeps a margin between four lines of text and the button
    private static readonly Vector2 PopupSize = new(579, 270);
    private static readonly Vector2 ArrowSize = new(244, 204);
    // The arrow art points down-left: its heading in screen radians, so a rotation is the difference
    private const float ArrowHeading = 2.356f;
    // The arrow hangs above the bar's right end and points down at it, clear of the green forecast
    // number that sits just to the right of the bar
    private static readonly Vector2 ArrowFromTarget = new(50, -170);
    private static readonly Vector2 PopupFromArrow = new(80, -20);
    private const float ScreenMargin = 20f;

    private const double TickSeconds = 0.25;
    private const int MaxTicks = 600;
    // Ticks of quiet after the last modal or banner before the popup lands
    private const int SettleTicks = 4;

    private static readonly HashSet<string> Pending = new();

    public static void Queue(Tip tip, Creature creature)
    {
        if (TestMode.IsOn || SaveManager.Instance is not { } save || save.SeenFtue(tip.Id)) return;
        if (!Pending.Add(tip.Id)) return;
        TaskHelper.RunSafely(ShowWhenClear(tip, creature));
    }

    // The base combat tutorial and the start banner both own the screen at turn 1, and the modal
    // container drops a second modal instead of queueing it, so poll until the way is clear
    private static async Task ShowWhenClear(Tip tip, Creature creature)
    {
        try
        {
            var settled = 0;
            for (var tick = 0; tick < MaxTicks; tick++)
            {
                await Wait(TickSeconds);
                if (!(CombatManager.Instance?.IsInProgress ?? false) || !creature.IsAlive) return;
                if (SaveManager.Instance.SeenFtue(tip.Id)) return;
                if (tip != Antitoxin && Pending.Contains(Antitoxin.Id)) continue;
                if (NModalContainer.Instance is not { OpenModal: null } modal
                    || NCombatRoom.Instance is not { } room
                    || room.GetChildren().OfType<NCombatStartBanner>().Any())
                {
                    settled = 0;
                    continue;
                }
                if (++settled < SettleTicks) continue;
                if (FindTarget(creature) is not { } target) return;
                Show(tip, target, modal);
                return;
            }
        }
        finally
        {
            Pending.Remove(tip.Id);
        }
    }

    // Cmd.Wait returns at once in Instant fast mode, which would turn the poll into a busy loop
    private static async Task Wait(double seconds)
    {
        var timer = ((SceneTree)Engine.GetMainLoop()).CreateTimer(seconds);
        await timer.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    private static Control? FindTarget(Creature creature)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node == null || !GodotObject.IsInstanceValid(node)) return null;
        var bar = node.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar")?.GetNodeOrNull<NHealthBar>("%HealthBar");
        if (bar == null) return null;
        return AntitoxinBarPatches.BarNode(bar) ?? bar.HpBarContainer;
    }

    private static void Show(Tip tip, Control target, NModalContainer modal)
    {
        var root = new NFtue { Name = "AlchemistFtue" };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var popup = new TextureRect
        {
            Name = "FtuePopup",
            Texture = Load<Texture2D>(PopupTexture),
            Material = Load<Material>(PopupMaterial),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Size = PopupSize,
        };
        root.AddChild(popup);

        var header = new MegaLabel
        {
            Name = "Header",
            Position = new Vector2(82, 20),
            Size = new Vector2(461, 56),
            VerticalAlignment = VerticalAlignment.Center,
            MaxFontSize = 26,
        };
        header.AddThemeFontOverride("font", Load<Font>(HeaderFont));
        header.AddThemeFontSizeOverride("font_size", 26);
        header.AddThemeColorOverride("font_color", new Color(0.937255f, 0.784314f, 0.317647f));
        header.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.12549f));
        header.AddThemeColorOverride("font_outline_color", new Color(0.33f, 0.2475f, 0f));
        header.AddThemeConstantOverride("shadow_offset_x", 5);
        header.AddThemeConstantOverride("shadow_offset_y", 4);
        header.AddThemeConstantOverride("outline_size", 12);
        popup.AddChild(header);

        var body = new MegaRichTextLabel
        {
            Name = "Description",
            // Four lines at the base size need the whole gap between the header and the button; with
            // autosize on, the base box would shrink the text to 17
            Position = new Vector2(37, 82),
            Size = new Vector2(507, 108),
            BbcodeEnabled = true,
            ScrollActive = false,
            AutoSizeEnabled = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.AddThemeFontOverride("normal_font", Load<Font>(BodyFont));
        body.AddThemeFontOverride("bold_font", Load<Font>(BodyBoldFont));
        foreach (var size in new[] { "normal_font_size", "bold_font_size", "bold_italics_font_size", "italics_font_size", "mono_font_size" })
            body.AddThemeFontSizeOverride(size, 22);
        body.AddThemeColorOverride("default_color", new Color(1f, 0.964706f, 0.886275f));
        body.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.25098f));
        body.AddThemeConstantOverride("line_separation", -2);
        body.AddThemeConstantOverride("shadow_offset_x", 3);
        body.AddThemeConstantOverride("shadow_offset_y", 2);
        popup.AddChild(body);

        var confirm = SceneHelper.Instantiate<NFtueConfirmButton>(ConfirmScene);
        confirm.AnchorLeft = 0.5f;
        confirm.AnchorRight = 0.5f;
        confirm.AnchorTop = 1f;
        confirm.AnchorBottom = 1f;
        confirm.OffsetLeft = -137.5f;
        confirm.OffsetRight = 137.5f;
        confirm.OffsetTop = -67f;
        confirm.OffsetBottom = 8f;
        confirm.GrowVertical = Control.GrowDirection.Begin;
        popup.AddChild(confirm);

        var arrow = new TextureRect
        {
            Name = "Arrow",
            Texture = Load<Texture2D>(ArrowTexture),
            Size = ArrowSize,
            PivotOffset = ArrowSize / 2f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.AddChild(arrow);

        // Like the base tips, the thing pointed at draws above the backstop for as long as the tip is up
        var display = Ancestor<NCreatureStateDisplay>(target);
        var defaultZ = display?.ZIndex ?? 0;
        if (display != null) display.ZIndex = defaultZ + 1 - EffectiveZ(display);
        root.Connect(Node.SignalName.TreeExited, Callable.From(() =>
        {
            if (display != null && GodotObject.IsInstanceValid(display)) display.ZIndex = defaultZ;
        }));
        confirm.Connect(NClickableControl.SignalName.Released,
            Callable.From((NButton _) => NModalContainer.Instance?.Clear()));

        modal.Add(root);
        SaveManager.Instance.MarkFtueAsComplete(tip.Id);

        // Text and layout wait for the tree: the labels size their font from the theme they find there
        header.SetTextAutoSize(new LocString("ftues", tip.LocKey + ".title").GetFormattedText());
        body.Text = new LocString("ftues", tip.LocKey + ".description").GetFormattedText();

        var origin = root.GlobalPosition;
        var view = root.GetViewportRect().Size;
        var aim = target.GetGlobalTransform() * new Vector2(target.Size.X, target.Size.Y / 2f);
        var arrowCenter = aim + ArrowFromTarget;
        arrow.Position = arrowCenter - ArrowSize / 2f - origin;
        arrow.Rotation = (aim - arrowCenter).Angle() - ArrowHeading;
        var popupPos = arrowCenter + PopupFromArrow + new Vector2(0f, -ArrowSize.Y / 2f - PopupSize.Y);
        popup.Position = popupPos.Clamp(new Vector2(ScreenMargin, ScreenMargin),
            view - PopupSize - new Vector2(ScreenMargin, ScreenMargin)) - origin;
    }

    private static T Load<T>(string path) where T : Resource =>
        ResourceLoader.Load<T>(path, null, ResourceLoader.CacheMode.Reuse);

    // The z the backstop competes with: every ancestor's z_index adds up while z_as_relative holds
    private static int EffectiveZ(CanvasItem item)
    {
        var z = 0;
        for (Node? node = item; node is CanvasItem ci; node = node.GetParent())
        {
            z += ci.ZIndex;
            if (!ci.ZAsRelative) break;
        }
        return z;
    }

    private static T? Ancestor<T>(Node? node) where T : Node
    {
        while (node != null && node is not T) node = node.GetParent();
        return node as T;
    }
}
