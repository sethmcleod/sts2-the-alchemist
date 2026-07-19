# CLAUDE.md, agent orientation

This file gives guidance to AI agents such as Claude Code that work on this repo. A person
can also do all of the tasks here without an agent. A person must start at
[README.md](README.md). The documents for people are the primary record, and this file
only points to them.

## What this is

This repo is a custom character mod for Slay the Spire 2. It uses C# and Godot, with
BaseLib and Harmony. It is also an example of STS2 modding for other authors.

The tools are in the [sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp)
toolkit. The toolkit has these parts:

- the MCP server
- the MCPTest game bridge on TCP 21337, and GodotExplorer on 27020
- the test engine

The general modding guides are in the toolkit. The conventions for this mod are in this
repo. In this document, "the bridge" means the MCPTest game bridge and GodotExplorer.
Other toolkits can be added later. For example,
[STS2MCP](https://github.com/Gennadiyev/STS2MCP) uses REST on port 15526 for play by an AI
and for local multiplayer. Thus you must not assume that sts2-modding-mcp is the only
connected server.

## Commands

```sh
scripts/setup.sh            # first-time: clone tooling, install bridge mods
scripts/dev.sh publish      # build → import → publish → verify pck  (asset/loc changes)
scripts/dev.sh publish-fast # code-only changes (skips godot import)
scripts/dev.sh test [--group G] [name] [--fresh]
                            # regression suite vs the live game; starts/restarts the
                            #   game itself. Groups: cards/ancients/shop/settings/compendium.
                            #   (agents: steps-format JSONs also work via
                            #   sts2-modding-mcp's run_test_scenario tool)
scripts/dev.sh game-start | game-stop | game-restart   # game process control (Steam must run)
scripts/dev.sh lint         # offline static three-way-rule check (no game)
scripts/dev.sh changelog    # draft CHANGELOG entries from commits since the last tag
scripts/dev.sh release <patch|minor|major|X.Y.Z>   # bump + roll changelog + package zip (see RELEASING.md)
scripts/dev.sh doctor       # ✓/✗ every prerequisite
```

Each change that a player can see needs an entry in `CHANGELOG.md` under
`## [Unreleased]`. Use `scripts/dev.sh release` to make a release. See
[RELEASING.md](RELEASING.md).

`dotnet build` must pass with 0 errors. The localization analyzer runs as part of the
build. It makes sure that each power has the `.title`, `.description`, and
`.smartDescription` keys.

## Rules you must not skip

- **Three-way update rule**: a card is in the code, the localization, and `cards.csv`. If
  you change one of the three, change all three. See [CONTRIBUTING.md](CONTRIBUTING.md).
  [docs/adding-a-card.md](docs/adding-a-card.md) has a full worked example.
- **Design and code conventions**: see [CONTRIBUTING.md](CONTRIBUTING.md). It gives the
  rules for real numbers and for the cost of an upgrade. It also gives the balance of risk
  and reward, the comment style, and the asset paths.
- **The description code and the tooltip code must not read `Owner` without a guard.** A
  canonical model (in the compendium) throws an exception. Use `IsMutable` as the guard
  (see `AlchemistCard.ShouldGlowGoldInternal`).

## Live tests against the game (bridge)

[scripts/tests/README.md](scripts/tests/README.md) lists the special conditions for a
scenario. [docs/troubleshooting.md](docs/troubleshooting.md) lists the runtime problems.
These problems take the most time to find:

- Use a **new game process** with a **spare save profile**. Never abandon a run during a
  combat. This breaks the combat initialization until you restart the game. Use the `die`
  console command to leave a combat instead.
- A custom entity needs the **full model id**, for example `card ALCHEMIST-SEPSIS`. The
  short name `Sepsis` does nothing, and it shows no error. To find a model id, use the
  console command `dump`. Then read the game log.
- For the console commands `power` and `damage`, the target index `0` is the player. The
  index `1` is the first enemy. These indexes are one more than the `target_index` of
  `play_card`, which starts at 0.
- **Hot reload** (tier 2 and tier 3) works only from the main menu. It also works only
  **before the first combat** in the session. After the first combat,
  `ModelIdSerializationCache` locks. Then you must restart the game.
- An assertion can read the player state and `enemy_N_hp`. An assertion **cannot read the
  power stacks of an enemy**.
- On macOS, a screen transition can stop while the game window does not have the focus.
  The reset procedure of the runner (`die` and ForceClick) does not need the focus.

## Doc map

| Doc | What is in it |
|---|---|
| [README.md](README.md) | design, mechanics, install, quickstart |
| [BUILD.md](BUILD.md) | prerequisites, build and publish, asset conventions |
| [CONTRIBUTING.md](CONTRIBUTING.md) | the three-way rule, design and code conventions |
| [RELEASING.md](RELEASING.md) | version policy, changelog, release and package steps |
| [docs/adding-a-card.md](docs/adding-a-card.md) | a full worked example |
| [docs/troubleshooting.md](docs/troubleshooting.md) | known problems and their fixes |
| [docs/backlog.md](docs/backlog.md) | improvements that are not scheduled, with the evidence for each |
| [docs/baselib-improvements.md](docs/baselib-improvements.md) | findings to send upstream to BaseLib |
| [scripts/tests/README.md](scripts/tests/README.md) | the regression suite, and the special conditions of the bridge |
