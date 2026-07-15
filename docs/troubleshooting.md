# Troubleshooting

Known gotchas, each paid for the hard way. Run `scripts/dev.sh doctor` first — it
catches the boring environment problems (missing SDK, missing mods, game not running).

## Build & publish

**`dotnet build` fails with localization analyzer errors**
Every power needs `.title`, `.description` *and* `.smartDescription` keys in
`Alchemist/localization/eng/powers.json`. This is intentional — fix the loc, not the
analyzer.

**My image/localization change doesn't show up in game**
`dotnet build` only ships code. Anything under `Alchemist/` (images, scenes, loc JSON)
ships inside the `.pck`, which only `scripts/dev.sh publish` rebuilds — and **new images
must be imported by Godot first** (`publish` does import → publish in the right order;
`publish-fast` skips the import on purpose).

**`dotnet` found but weird runtime errors**
The repo pins net9 while newer SDKs may be installed; `dev.sh` exports
`DOTNET_ROLL_FORWARD=Major` for this. If you build outside `dev.sh`, export it yourself.

## Live testing (the sts2-modding-mcp bridge: MCPTest :21337 + GodotExplorer :27020)

**Combats stop initializing — enemies appear but no hand/energy/background**
The game process is poisoned: something abandoned a run **mid-combat** earlier in this
session. Every later fight half-loads, no matter how it's started. Restart the game.
(The test runner avoids this by `die`-ing out of combats instead; do the same in manual
sessions.)

**`card <Name>` / `power <Name>` console commands silently do nothing**
Custom entities need **full model IDs**: `card ALCHEMIST-SEPSIS`, not `card Sepsis`.
The console reports success either way. Discover IDs with the `dump` console command
(writes the model DB to the game log).

**Poison/damage landed on the wrong target from console commands**
Console `power`/`damage`/`block` use `0` = player, `1` = first enemy, `2` = second…
That's offset by one from `play_card`-style enemy indices (0-based).

**Hot reload fails with `ModelIdSerializationCache ... initonly` error**
Entity/localization hot reload only works from the main menu **before any combat has
happened in this game session** — the serialization cache locks at first combat and
can't be rewritten. Restart the game to load new entity code.

**Scene transitions hang while testing (macOS)**
The game may not process screen transitions while its window is unfocused, and the
Windows-only focus workaround doesn't exist on macOS. Click the game window, or stick to
the focus-independent paths the test runner uses (console commands + explorer
ForceClick).

**Test suite fails immediately / `bridge not reachable`**
The game must be running via Steam with the `mcptest` and `godotexplorer` mods installed
(`scripts/dev.sh bridge`), on a **fresh process** at the main menu. `scripts/dev.sh
doctor` shows exactly what's missing. Use a spare save profile — the suite starts and
abandons runs constantly, which advances Timeline progression.

**A damage-based test passes sometimes and fails other times**
Encounter rosters are **not** seed-stable (`fight SLIMES_WEAK` re-rolls enemy HP each
run). Assert player state, block, or fixed quantities — not absolute enemy HP. See
[scripts/tests/README.md](../scripts/tests/README.md).

## Where else to look

- Environment/build basics: [BUILD.md](../BUILD.md)
- Suite prerequisites + scenario-authoring quirks: [scripts/tests/README.md](../scripts/tests/README.md)
- Generic toolkit issues (MCP server, decompilation, bridge internals):
  [sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) docs
