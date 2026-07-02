# Building The Alchemist

## Prerequisites
- **.NET 9 SDK**
- **Slay the Spire 2** installed via Steam (the build auto-discovers the install path — see `Sts2PathDiscovery.props`)
- **MegaDot 4.5.1** (Godot fork used by STS2) — required only for `dotnet publish` (asset/.pck export). Set its path in `Directory.Build.props` (`<GodotPath>`) if yours differs.

## Commands
- `dotnet build` — compiles `Alchemist.dll` and copies it (plus `Alchemist.json`) into the game's `mods/Alchemist/` folder. Enough for **code-only** changes.
- `dotnet publish -c Debug` — additionally exports `Alchemist.pck` (images, scenes, **localization JSON**). Required whenever anything under `Alchemist/` changes.
- New/changed images must be imported by Godot before publishing:
  `"<GodotPath>" --headless --import --path .`

## Conventions
- Cards/powers/relics/potions derive their localization keys and icon filenames from the class name (`MyCard` → `ALCHEMIST-MY_CARD`, `my_card.png`). Icon sizes: powers 64/256(big), relics 94/94(outline)/256(big), card portraits in `card_portraits/big/`.
- Every power needs `.title`, `.description` **and** `.smartDescription` loc keys (build analyzer enforces this).
- `cards.csv` is the design source of truth — update it whenever a card's stats/text change.
- Never place mod assets at base-game paths (`res://images/...`); keep everything under `res://Alchemist/` (see `Patches/RestSitePatches.cs` for why).
