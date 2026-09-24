using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using MegaCrit.Sts2.Core.Saves;
using FileAccess = Godot.FileAccess;

namespace Alchemist.AlchemistCode.Config;

/// <summary>
/// Whether the Konami code has opened the Secrets section on the current save profile.
/// </summary>
/// <remarks>
/// The flag is a file in the profile folder and each profile has its own. The cloud sync of the
/// game reads only the files it names and leaves this file alone.
/// </remarks>
internal static class SecretUnlocks
{
    private const string FileName = "alchemist.json";
    // Profiles already store this key, and a new name would lock them again
    private const string UnlockedKey = "cheatsUnlocked";

    private static string? _readPath;
    private static bool _unlocked;

    public static bool IsUnlocked
    {
        get
        {
            var path = FilePath();
            if (path == null) return false;

            if (path != _readPath)
            {
                _unlocked = Read(path);
                _readPath = path;
            }

            return _unlocked;
        }
    }

    public static bool Unlock()
    {
        var path = FilePath();
        if (path == null) return false;

        DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            MainFile.Logger.Error($"Could not save the secrets unlock to {path} ({FileAccess.GetOpenError()}).");
            return false;
        }

        file.StoreString(new JsonObject { [UnlockedKey] = true }.ToJsonString());
        _readPath = path;
        _unlocked = true;
        return true;
    }

    private static bool Read(string path)
    {
        if (!FileAccess.FileExists(path)) return false;

        try
        {
            return JsonNode.Parse(FileAccess.GetFileAsString(path))?[UnlockedKey]?.GetValue<bool>() ?? false;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException)
        {
            MainFile.Logger.Error($"Could not read {path}, thus the secrets stay locked: {e.Message}");
            return false;
        }
    }

    private static string? FilePath()
    {
        if (SaveManager.Instance is not { IsProfileInitialized: true } save) return null;

        return UserDataPathProvider.GetProfileScopedBasePath(save.CurrentProfileId) + "/" + FileName;
    }
}
