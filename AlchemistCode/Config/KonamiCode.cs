using Godot;

namespace Alchemist.AlchemistCode.Config;

/// <summary>
/// Watches for ↑ ↑ ↓ ↓ ← → ← → B A on the keyboard or a controller while a page is in the tree.
/// </summary>
/// <remarks>
/// The root window raises window_input for each event before any node sees it, thus no control on
/// the page can accept an arrow press first. The events go on to the game untouched.
/// </remarks>
internal static class KonamiCode
{
    private enum Press { Other, Up, Down, Left, Right, B, A }

    private static readonly Press[] Sequence =
    [
        Press.Up, Press.Up, Press.Down, Press.Down,
        Press.Left, Press.Right, Press.Left, Press.Right,
        Press.B, Press.A
    ];

    public static void Listen(Node page, Action onEntered)
    {
        if (!page.IsInsideTree()) return;

        var root = page.GetTree().Root;
        var recent = new Queue<Press>(Sequence.Length);

        Window.WindowInputEventHandler handler = inputEvent =>
        {
            if (ReadPress(inputEvent) is not { } press) return;

            recent.Enqueue(press);
            if (recent.Count > Sequence.Length) recent.Dequeue();
            if (!recent.SequenceEqual(Sequence)) return;

            recent.Clear();
            Callable.From(onEntered).CallDeferred();
        };

        root.WindowInput += handler;
        page.TreeExiting += () =>
        {
            if (GodotObject.IsInstanceValid(root)) root.WindowInput -= handler;
        };
    }

    // Mouse events, stick motion and the menu's own echoed actions don't break the sequence
    private static Press? ReadPress(InputEvent inputEvent) => inputEvent switch
    {
        InputEventKey { Pressed: true, Echo: false } key => key.Keycode switch
        {
            Key.Up => Press.Up,
            Key.Down => Press.Down,
            Key.Left => Press.Left,
            Key.Right => Press.Right,
            Key.B => Press.B,
            Key.A => Press.A,
            _ => Press.Other
        },
        InputEventJoypadButton { Pressed: true } button => button.ButtonIndex switch
        {
            JoyButton.DpadUp => Press.Up,
            JoyButton.DpadDown => Press.Down,
            JoyButton.DpadLeft => Press.Left,
            JoyButton.DpadRight => Press.Right,
            JoyButton.B => Press.B,
            JoyButton.A => Press.A,
            _ => Press.Other
        },
        _ => null
    };
}
