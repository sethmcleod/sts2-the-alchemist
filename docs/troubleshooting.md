# Troubleshooting

Known problems and their fixes.

> [!TIP]
> Run `scripts/dev.sh doctor` first. It finds the simple environment problems before you
> look at the items below. These problems include a missing SDK, missing mods, or a game
> that does not run.

## Build & publish

**`dotnet build` fails with localization analyzer errors**
Every power needs the `.title`, `.description` *and* `.smartDescription` keys. Put these
keys in `Alchemist/localization/eng/powers.json`. This behavior is intentional. Correct
the localization. Do not change the analyzer.

**An image change or a localization change does not show in the game**
The `dotnet build` command builds only the code. All files under `Alchemist/` go into
the `.pck` file. These files include images, scenes and localization JSON. Only
`scripts/dev.sh publish` builds the `.pck` file again.

Also, Godot must import **new images** first. The `publish` command does the import, then
the publish, in the correct sequence. The `publish-fast` command skips the import on
purpose.

**The `dotnet` command is found, but runtime errors happen**
The repository pins net9. Newer SDKs can also be installed. For this reason, `dev.sh`
exports `DOTNET_ROLL_FORWARD=Major`. Export this variable yourself if you build outside
`dev.sh`.

**Combat shows no background, then stops with an `AssetLoadException`**
You published the mod while the game ran. Godot keeps the `.pck` file open. If you
replace the file under a live process, all later asset loads from that file fail:

```
AssetLoadException: Asset previously failed to load:
  res://Alchemist/scenes/combat/energy_counters/alchemist_energy_counter.tscn.
  The game installation may be corrupted.
    at NEnergyCounter.Create → NCombatUi.Activate → NCombatRoom.OnCombatSetUp
```

The custom energy counter is only the first asset that combat requests. The exception
comes out of `NCombatUi.Activate`. The combat UI setup then stops when it is half built.
The result is no background. A game-over screen then tries to animate a UI that does not
exist. This causes a `NullReferenceException` in `NCombatUi.AnimOut()`.

Nothing is corrupted. Players never see this problem. To correct it, run
`scripts/dev.sh game-restart`. The `publish` command now gives a warning if the game is
in operation.

**A C# script on a scene root does not run when the game instantiates the scene**
A mod `.tscn` can reference a mod C# script as a `Script` ext_resource. This works when
BaseLib converts the scene (the energy counter). It fails when game code instantiates
the scene directly with `PackedScene.Instantiate`. An example is the character-select
background. The engine log then repeats this error, and `_Ready` never runs:

```
ERROR: System.ArgumentException: Undefined resource string ID:0x80070057
   at <YourClass>.InvokeGodotClassMethod(...)
   at Godot.Bridge.CSharpInstanceBridge.Call(...)
```

A `try/catch` inside `_Ready` does not catch this. The failure is in the method
dispatch, before the method body. The fix: remove the script from the scene. Apply the
runtime setup from a Harmony postfix on the game method that instantiates the scene.
See `CharSelectBgPatches` for the pattern.

## Live tests (the sts2-modding-mcp bridge: MCPTest :21337 + GodotExplorer :27020)

**Combats do not initialize: enemies appear, but the hand, energy and background do not**
The game process is in a bad state. Someone abandoned a run **during combat** earlier in
this session. Every later fight loads only half of its content. The start method does not
change this result.

Restart the game. The test runner prevents this problem, because it uses the `die`
command to leave combats. Do the same in manual sessions.

**The `card <Name>` and `power <Name>` console commands do nothing**
Custom entities need the **full model id**. Use `card ALCHEMIST-SEPSIS`, not
`card Sepsis`. The console reports success for both commands. To find the model ids, use
the `dump` console command. This command writes the model database to the game log.

**A console command applied Poison or damage to the wrong target**
The console `power`, `damage` and `block` commands use `0` for the player. They use `1`
for the first enemy, `2` for the second enemy, and so on. These indices have an offset of
one from the enemy indices of `play_card`, which start at `0`.

**Hot reload fails with `ModelIdSerializationCache ... initonly` error**
Hot reload of entities and localization works only from the main menu. It also works only
**before the first combat in this game session**. The serialization cache locks at the
first combat. You cannot write to it again. Restart the game to load new entity code.

**Scene transitions stop during a test (macOS)**
The game can fail to process screen transitions when its window does not have focus. The
focus workaround for Windows does not exist on macOS. Click the game window. As an
alternative, use only the paths that do not need focus. The test runner uses these paths:
console commands and the explorer ForceClick method.

**The test suite fails immediately, or reports `bridge not reachable`**
Start the game from Steam. Install the `mcptest` and `godotexplorer` mods with
`scripts/dev.sh bridge`. Use a **new game process** at the main menu. The
`scripts/dev.sh doctor` command shows which items are missing.

Use a spare save profile. The suite starts and abandons many runs. This advances the
Timeline progression.

**A damage-based test passes sometimes and fails other times**
Encounter rosters are **not** seed-stable. The `fight SLIMES_WEAK` command sets new enemy
HP values on each run. Do not assert the absolute enemy HP. Assert the player state, the
block, or fixed quantities. See [scripts/tests/README.md](../scripts/tests/README.md).

## Where else to look

- Environment and build basics: [BUILD.md](../BUILD.md)
- Suite prerequisites and problems when you write scenarios:
  [scripts/tests/README.md](../scripts/tests/README.md)
- General toolkit problems (MCP server, decompilation, bridge internals):
  [sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) documentation
