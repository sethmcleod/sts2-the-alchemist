---
name: playtest
description: Run the mod against the live Slay the Spire 2 game safely, whether to run the regression suite, spawn a card to check it by hand, or verify a change in a real run. Use this whenever work needs the running game, since driving it has sharp edges that cost real time when missed: publishing over a live process corrupts its asset loads, testing on the wrong save profile churns real progress, and abandoning a run mid-combat poisons combat init until restart. Encodes the safe loop and the recovery steps.
---

# Playtest against the live game

The suite and manual checks drive a **real running game** over the bridge (MCPTest on
:21337, GodotExplorer on :27020). The mechanics are documented in
[scripts/tests/README.md](../../../scripts/tests/README.md) and the failure modes in
[docs/troubleshooting.md](../../../docs/troubleshooting.md). This skill is the safe
operating procedure; follow it rather than driving the game ad hoc.

## Before you touch the game

- **Check the environment.** `scripts/dev.sh doctor` prints a ✓/✗ for every prerequisite
  (SDK, mods installed, bridge responding, Steam running). Start here if anything looks off.
- **Confirm the save profile is a spare.** The suite starts and abandons runs constantly
  and the settings tests exercise unlock/relock, so it must run on **Profile 3, never
  Profile 1**. Pointing it at a real save churns that save's progression.

## The one rule that bites hardest: never publish over a running game

Godot holds the mod's `.pck` open while the game runs. Replacing it under the live process
invalidates every later asset load from it, and combat then renders with **no background**
and later throws `AssetLoadException` / `NullReferenceException`. Nothing is actually
corrupted, but the session is poisoned.

So: **publish, then start or restart the game**, not the other way around. If you have
already published over a running game (or a run is behaving strangely), the fix is a
restart:

```sh
scripts/dev.sh game-restart
```

## Running the suite

```sh
scripts/dev.sh test                 # everything
scripts/dev.sh test --group cards   # one group while iterating
scripts/dev.sh test <name>          # scenarios whose filename contains <name>
scripts/dev.sh test --changed       # only the groups your uncommitted edits can affect
scripts/dev.sh test --fresh         # force a game restart first, when state is suspect
```

The runner starts the game if it is not up and restarts it if it wedges, so you usually do
not manage the process yourself. Reach for `--fresh` when a green change starts failing for
no code reason; that is almost always stale process state, and this session has hit it.

## Driving the game by hand

- Spawn custom entities with **full model ids**: `card ALCHEMIST-SEPSIS`, not `Sepsis`.
  Bare names silently no-op while the console still reports success.
- To end a combat, use `die`, **never abandon a run mid-combat**. Abandoning mid-combat
  poisons combat init for the rest of the process, so every later fight half-loads until a
  restart. The suite follows this rule; so should you.
- Console `power`/`damage` target index is offset by one from `play_card`: `0` = player,
  `1` = first enemy.

## When you are done

Leave the game the way it's kept between sessions: **at the main menu, at 100% game speed.** The suite
runs at a faster speed, so if you ran it or set the speed up, reset it. Exit any live run
cleanly (Save and Quit, then abandon from the menu); never abandon a live run directly,
which stalls on the macOS death screen.

A good end state is: menu, 100% speed, no run in progress.
