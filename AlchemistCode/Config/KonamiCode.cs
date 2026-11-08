using Godot;

namespace Alchemist.AlchemistCode.Config;

// The root's window_input sees each event before any node, so no control can swallow an arrow press first
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
