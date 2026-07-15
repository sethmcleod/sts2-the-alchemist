# CLAUDE.md — agent orientation

Guidance for AI agents (Claude Code or similar) working on this repo. Everything here
is also doable without agents — humans should start at [README.md](README.md); the
human docs are the source of truth and this file just points at them.

## What this is

A custom Slay the Spire 2 character mod (C# / Godot, via BaseLib + Harmony), built as a
reference-quality example of STS2 modding. Tooling lives in the
[sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) toolkit (MCP server;
the MCPTest game bridge on TCP 21337 + GodotExplorer on 27020; the test engine) —
generic modding guides live there, mod-specific conventions live here. When this doc
says "the bridge," it means those two. Other toolkits may be integrated later (e.g.
[STS2MCP](https://github.com/Gennadiyev/STS2MCP), REST on :15526, for AI-piloted play
and local multiplayer) — don't assume sts2-modding-mcp is the only server connected.

## Commands

```sh
scripts/setup.sh            # first-time: clone tooling, install bridge mods
scripts/dev.sh publish      # build → import → publish → verify pck  (asset/loc changes)
scripts/dev.sh publish-fast # code-only changes (skips godot import)
scripts/dev.sh test [name]  # regression suite vs the live game (agents: same JSONs work
                            #   via sts2-modding-mcp's run_test_scenario tool)
scripts/dev.sh doctor       # ✓/✗ every prerequisite
```

`dotnet build` must pass with 0 errors — the in-build loc analyzer enforces that every
power has `.title`/`.description`/`.smartDescription` keys.

## Rules that bite if you skip them

- **Three-way update rule** — a card lives in code + localization + `cards.csv`; touch
  one, touch all. See [CONTRIBUTING.md](CONTRIBUTING.md). A worked end-to-end example is
  in [docs/adding-a-card.md](docs/adding-a-card.md).
- **Design & code conventions** — [CONTRIBUTING.md](CONTRIBUTING.md) (show real numbers,
  upgrades never raise cost, risk/reward pricing, comment style, asset paths).
- **Description/tooltip code must not read `Owner`** unguarded — canonical models
  (compendium) throw. Gate with `IsMutable` (see `AlchemistCard.ShouldGlowGoldInternal`).

## Live testing against the game (bridge)

Scenario-authoring quirks: [scripts/tests/README.md](scripts/tests/README.md); runtime
gotchas: [docs/troubleshooting.md](docs/troubleshooting.md). The ones that cost the most
time to discover:

- Use a **fresh game process** on a **spare save profile**; never abandon a run
  mid-combat (poisons combat init until restart — `die` out of combats instead).
- Custom entities need **full model IDs**: `card ALCHEMIST-SEPSIS`, not `Sepsis`.
  Bare names silently no-op. Discover IDs via console `dump` + the game log.
- Console `power`/`damage` target index: `0` = player, `1` = first enemy (offset by one
  vs `play_card`'s 0-based `target_index`).
- **Hot reload** (tier 2/3) only works from the main menu **before any combat** that
  session (`ModelIdSerializationCache` locks); after that, restart the game.
- Assertions can read player state and `enemy_N_hp` — **not enemy power stacks**.
- macOS: screen transitions may stall while the game window is unfocused; the runner's
  reset recipe (`die` + ForceClick) is focus-independent.

## Doc map

| Doc | What's in it |
|---|---|
| [README.md](README.md) | design, mechanics, install, quickstart |
| [BUILD.md](BUILD.md) | prerequisites, build/publish, asset conventions |
| [CONTRIBUTING.md](CONTRIBUTING.md) | three-way rule, design + code conventions |
| [docs/adding-a-card.md](docs/adding-a-card.md) | end-to-end worked example |
| [docs/troubleshooting.md](docs/troubleshooting.md) | known gotchas + fixes |
| [scripts/tests/README.md](scripts/tests/README.md) | regression suite + bridge quirks |
