# How to build The Alchemist

## Prerequisites

- **.NET 9 SDK**
- **Slay the Spire 2**, installed with Steam. The build finds the install path automatically (see `Sts2PathDiscovery.props`).
- **Godot 4.5.1 (.NET/mono build)**. You need Godot only for `dotnet publish`, which exports the assets and the pck. MegaDot, the Godot fork from MegaCrit, also works. Get a standard build from [godotengine.org/download/archive](https://godotengine.org/download/archive/). Select the “.NET” build.

  > [!IMPORTANT]
  > The **4.5.1 version match** is necessary. The game does not load a `.pck` file that a newer Godot exported. There is no error message. The assets of the mod do not appear.

  The path to Godot is in **two places**. If your path is not the default path, change both:
  - `Directory.Build.props` `<GodotPath>`: `dotnet publish` uses it for the pck export.
  - the `GODOT` env var: `scripts/dev.sh import` uses it for the image import step.

## Commands

- `dotnet build` compiles `Alchemist.dll`. It copies `Alchemist.dll` and `Alchemist.json` into the `mods/Alchemist/` folder of the game. This is enough for a change to the **code only**.
- `dotnet publish -c Debug` also exports `Alchemist.pck`, which contains the images, the scenes, and the **localization JSON**. Use this command when you change any file in `Alchemist/`.
- Godot must import each new image and each changed image before you publish:
  `"<GodotPath>" --headless --import --path .`
- Or use `scripts/dev.sh publish`, which does all of the steps above in the correct sequence.

## Testing against the game's default branch

The mod ships from two git branches, because the game has two Steam branches whose
DLLs are not compatible (see [RELEASING.md](RELEASING.md)). `beta` is where you work.
You only need this procedure when you promote to `main`, which is rare.

**The whole hazard in one sentence:** `Alchemist.dll` is compiled against one
`sts2.dll`, and loading it on the other Steam branch throws
`ReflectionTypeLoadException` at startup with nothing to say about why. Three things
must agree — the git branch, the Steam branch, and the build the installed mod was
compiled against — and only the third is invisible.

`scripts/dev.sh doctor` checks all three and names the fix. `scripts/dev.sh env`
prints them. Both read Steam's own `appmanifest_2868840.acf`, where `BetaKey` is
`public-beta` on the beta branch and absent on the default branch.

To test on the default branch:

1. In Steam, right-click Slay the Spire 2 → Properties → Betas, and select **None**.
   Let it download. This replaces the install in place; there is no second copy.
2. `git switch main`
3. `scripts/dev.sh publish` — **this step is not optional.** The installed
   `Alchemist.dll` is still the beta-branch build until you rebuild, and it will not
   load. The build pins `Sts2Path` to `STS2_GAME_DIR`, so it compiles against
   whichever branch is installed right now.
4. `scripts/dev.sh doctor` to confirm all three agree, then run the game. Loading to
   the character select screen at all is the real test; a DLL mismatch fails before
   anything else can.

To go back: reverse steps 1 and 2, then `scripts/dev.sh publish` again.

If you would rather keep both branches installed at once, put the second copy
anywhere and point the tooling at it:

```sh
STS2_GAME_DIR="/path/to/the/other/Slay the Spire 2" scripts/dev.sh publish
```

Everything downstream follows that variable, including the compile reference. The
branch and build detection reads the `appmanifest` next to that install, so `doctor`
stays honest about which copy you are on.

## CI

`.github/workflows/lint.yml` runs on each push and each PR. It does the three-way rule
check. It also checks the localization JSON. It does no more than this.

A compile needs `sts2.dll` from a Steam install. A public runner cannot have this file.
Thus the build fails without the game (`Sts2PathDiscovery.props`). The compile runs on
the local machine instead. `scripts/dev.sh release` starts it (see
[RELEASING.md](RELEASING.md)).

## Conventions

- The class name gives the localization keys and the icon file names for each card, power, relic, and potion. For example, `MyCard` gives `ALCHEMIST-MY_CARD` and `my_card.png`.
- The icon sizes are:
  - power: 64 and 256 (big)
  - relic: 94, 94 (outline), and 256 (big)
- Put the card portraits in `card_portraits/`, and the beta placeholders in `card_portraits/beta/`. `CardImageOrBetaPath` prefers the real art, so a portrait can land one card at a time (see [docs/beta-card-art.md](docs/beta-card-art.md)).
- Each power needs the localization keys `.title`, `.description`, **and** `.smartDescription`. The localization analyzer in the build checks this.
- The file `cards.csv` is the primary record of the design. Update it when you change the stats or the text of a card.

> [!CAUTION]
> Never put the mod assets at base game paths (`res://images/...`). Keep all of them in `res://Alchemist/` (see `Patches/RestSitePatches.cs` for the reason).

### Audio

- Put sound files in `Alchemist/audio/`. The pck carries them like any other asset.
- To play a custom sound, use a `res://` path where the game expects an FMOD event path. For example, `CharacterSelectSfx` returns `res://Alchemist/audio/alchemist_select.wav`. BaseLib `PlayResourcePatch` detects the `res://` prefix and plays the file through Godot audio players. The volume sliders apply.
- Do not use `BaseLib.Utils.FmodAudio`. It is obsolete, and its replacement hook does not intercept all play calls. `BaseLib.Audio.ModAudio` is the maintained API for direct playback.
- To reuse a base game sound, pass its `event:/` path to `SfxCmd.Play`. The event paths appear in the game's decompiled source as `"event:/..."` strings.
- `scripts/gen_select_sfx.py` generates the character select sound. It documents the synthesis parameters. Match the loudness of new sounds to the base game: the select clips sit near -16 dB RMS. The script uses soft saturation (`drive`) to reach that level, because peak normalization alone is not enough for sparse material.
