# Building The Alchemist

## Prerequisites
- **.NET 9 SDK**
- **Slay the Spire 2** installed via Steam (the build auto-discovers the install path, see `Sts2PathDiscovery.props`)
- **Godot 4.5.1 (.NET/mono build)**, required only for `dotnet publish` (asset/.pck export). MegaDot (MegaCrit's Godot fork) works too. Standard builds: [godotengine.org/download/archive](https://godotengine.org/download/archive/) (pick “.NET”).

  > [!IMPORTANT]
  > What matters is the **4.5.1 version match**. The game won't load a `.pck` exported by a newer Godot, and the failure is silent: the mod's assets simply don't appear.

  Its path lives in **two places** (set both if yours differs from the default):
  - `Directory.Build.props` `<GodotPath>`, used by `dotnet publish` for the pck export
  - the `GODOT` env var, used by `scripts/dev.sh import` for the image-import step

## Commands
- `dotnet build` compiles `Alchemist.dll` and copies it (plus `Alchemist.json`) into the game's `mods/Alchemist/` folder. Enough for **code-only** changes.
- `dotnet publish -c Debug` additionally exports `Alchemist.pck` (images, scenes, **localization JSON**). Required whenever anything under `Alchemist/` changes.
- New/changed images must be imported by Godot before publishing:
  `"<GodotPath>" --headless --import --path .`
- Or let `scripts/dev.sh publish` chain all of the above in the right order.

## Conventions
- Cards/powers/relics/potions derive their localization keys and icon filenames from the class name (`MyCard` → `ALCHEMIST-MY_CARD`, `my_card.png`). Icon sizes: powers 64/256(big), relics 94/94(outline)/256(big), card portraits in `card_portraits/big/`.
- Every power needs `.title`, `.description` **and** `.smartDescription` loc keys (build analyzer enforces this).
- `cards.csv` is the design source of truth. Update it whenever a card's stats/text change.

> [!CAUTION]
> Never place mod assets at base-game paths (`res://images/...`); keep everything under `res://Alchemist/` (see `Patches/RestSitePatches.cs` for why).
