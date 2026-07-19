---
name: playtest
description: Run the mod against the live Slay the Spire 2 game safely. Use this skill to run the regression suite, to spawn a card and check it by hand, or to verify a change in a real run. Use it for any task that needs the live game. The game has three hazards that cost real time. A publish over a live process corrupts the asset loads of that process. A test on the wrong save profile changes real progress. If you abandon a run during a combat, every later combat fails to initialize until you restart the game. This skill gives the safe loop and the recovery steps.
---

# Playtest against the live game

The suite and the manual checks drive a **real live game** through the bridge (MCPTest on
:21337, GodotExplorer on :27020).
[scripts/tests/README.md](../../../scripts/tests/README.md) documents the mechanics.
[docs/troubleshooting.md](../../../docs/troubleshooting.md) documents the failure modes.
This skill is the safe procedure. Follow it. Do not drive the game without it.

## Before you touch the game

- **Check the environment.** The command `scripts/dev.sh doctor` prints a ✓ or a ✗ for
  each prerequisite. It checks the SDK, the installed mods, the bridge response, and the
  Steam process. Start here if something is not correct.
- **Confirm that the save profile is a spare profile.** The suite starts runs and abandons
  runs many times. The settings tests also lock and unlock content. Thus the suite must
  run on **Profile 3, never Profile 1**. If you point the suite at a real save, it changes
  the progression of that save.

## The most important rule, never publish over a live game

Godot keeps the `.pck` file of the mod open while the game runs. If you replace that file
under the live process, every later asset load from it fails. Combat then shows **no
background**. The game later throws `AssetLoadException` or `NullReferenceException`. No
file is corrupted, but the session is no longer usable.

Follow this order. **Publish first.** **Then start or restart the game.** Do not use the
opposite order. If you already published over a live game, restart the game. Also restart
the game if a run behaves in an unusual way.

```sh
scripts/dev.sh game-restart
```

## How to run the suite

```sh
scripts/dev.sh test                 # everything
scripts/dev.sh test --group cards   # one group while iterating
scripts/dev.sh test <name>          # scenarios whose filename contains <name>
scripts/dev.sh test --changed       # only the groups your uncommitted edits can affect
scripts/dev.sh test --fresh         # force a game restart first, when state is suspect
```

The runner starts the game if the game is not up. The runner restarts the game if the game
becomes stuck. Thus you usually do not control the process yourself. Use `--fresh` when a
change that passed before starts to fail with no change in the code. The cause is almost
always old process state. This problem happens in practice.

## How to drive the game by hand

- Spawn custom entities with a **full model id**. Use `card ALCHEMIST-SEPSIS`, not
  `Sepsis`. A bare name does nothing, but the console still reports success.
- To end a combat, use `die`. **Never abandon a run during a combat.** If you abandon a
  run during a combat, every later combat in that process fails to initialize. Each later
  fight loads only in part until you restart the game. The suite obeys this rule. You must
  also obey it.
- The target index for the console `power` and `damage` commands has an offset of one from
  `play_card`. The value `0` is the player. The value `1` is the first enemy.

## When you are done

Leave the game in the usual condition between sessions. That condition is **the main menu
at 100% game speed**. The suite runs at a higher speed. If you ran the suite, set the speed
back to 100%. If you raised the speed by hand, set it back to 100%.

Exit any live run correctly:
- Use Save and Quit.
- Then abandon the run from the main menu.
- Never abandon a live run directly, because the macOS death screen stalls.

The correct end state has three conditions. The game is at the main menu. The speed is
100%. No run is in progress.
