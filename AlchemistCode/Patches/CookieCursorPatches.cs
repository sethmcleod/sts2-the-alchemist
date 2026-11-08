using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Alchemist.AlchemistCode.Extensions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Alchemist.AlchemistCode.Patches;

// CookieCursor reads Cursors/<class>/icon.png from disk, never a pck, in an NMainMenu._Ready postfix
// This prefix writes the file before that scan
public static class CookieCursorPatches
{
    private const string CookieCursorModId = "CookieCursor";
    private const string CookieFile = "icon.png"; // os path
    private const string StampFile = "icon.alchemist"; // os path
    private static bool _done;

    [HarmonyPatch(typeof(NMainMenu), "_Ready")]
    public static class ExportCookie
    {
        public static void Prefix()
        {
            if (_done) return;
            _done = true;
            try
            {
                Export();
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Warn($"CookieCursor cookie export failed: {e.Message}");
            }
        }
    }

    // An icon whose hash doesn't match the stamp was placed by hand, so it's left alone
    private static void Export()
    {
        var mod = ModManager.GetLoadedMods().FirstOrDefault(m => m.manifest?.id == CookieCursorModId);
        if (mod == null) return;
        var root = Directory.Exists(mod.path) ? mod.path : Path.GetDirectoryName(mod.path);
        if (root == null) return;
        var dir = $"{root}/Cursors/alchemist";
        var file = $"{dir}/{CookieFile}";
        var stamp = $"{dir}/{StampFile}";
        var art = "alchemist_cookie.png".BigRelicImagePath();
        var image = RelicBigOutlinePatches.ComposeImage(art) ?? ResourceLoader.Load<Texture2D>(art)?.GetImage();
        if (image == null) return;
        if (image.IsCompressed()) image.Decompress();
        var png = image.SavePngToBuffer();
        var hash = System.Convert.ToHexString(SHA256.HashData(png));
        if (File.Exists(file))
        {
            var written = File.Exists(stamp) ? File.ReadAllText(stamp).Trim() : null;
            if (written == null) return;
            if (written == hash) return;
            if (System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) != written) return;
        }
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(file, png);
        File.WriteAllText(stamp, hash);
        MainFile.Logger.Info($"Wrote the Alchemist cookie for CookieCursor to {file}");
    }
}
