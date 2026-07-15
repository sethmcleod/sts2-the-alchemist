# Regression test suite

Seeded test scenarios that catch breakage when changing the mod — cards, mechanics,
localization, registration, and the mod's own UI. **No AI tooling required** — the suite
runs with plain Python against the live game, and manages the game process itself
(starts it if it's not running, restarts it if it crashes or wedges).

## Running the suite

```sh
scripts/setup.sh                    # once: clones tooling, installs the bridge mods
scripts/dev.sh test                 # run everything
scripts/dev.sh test --group shop    # one group
scripts/dev.sh test sepsis          # scenarios whose filename contains "sepsis"
scripts/dev.sh test --fresh         # force a game restart first (suspect state)
scripts/dev.sh test --speed 1       # watchable speed (default 3; >3 glitches the hand animation cosmetically)
scripts/dev.sh test --no-batch      # disable run-batching (see below) for debugging
```

Also: `scripts/dev.sh game-start | game-stop | game-restart` manage the game process
directly (Steam must be running; the game launches via the `steam://` protocol).
`scripts/dev.sh doctor` checks every prerequisite.

**Speed / batching.** Combat-only `checks` scenarios (setup is just a `SLIMES_WEAK`
fight) are *batched*: they share one run and reset via a fresh `fight` between them —
no menu round-trip, death animation, or Neow per test. This is the main time saver
(the cards group runs ~2.4× faster batched). Everything else resets to the menu with
its own setup. Batching is automatic; opt a scenario out with `"batch": false`. The
`cards_sweep` (~3 min, plays all 86 cards) dominates the full-suite time — use
`--group`/individual filters while iterating, and run the whole suite once per session.

This is a **stateful live-game** suite: each group passes in isolation, and a very long
full run may occasionally hit a rare UI-timing flake (e.g. the shop potion popup) — just
re-run that group.

Prerequisites: game installed, `mcptest` + `godotexplorer` bridge mods current
(`scripts/dev.sh bridge` — the compendium group needs a bridge built from a checkout
that has the `get_compendium` RPC), Alchemist mod published, and Python ≥ 3.10. The
suite pulls in no third-party packages (it drives the bridge over TCP with the stdlib),
so any 3.10+ interpreter works. The easiest way to get one — and what the sts2-modding-mcp
toolkit uses — is [uv](https://astral.sh/uv): install it and `scripts/dev.sh` provisions
Python for you automatically (it still prefers a system `python3` if one's already on PATH).
The game boots into the **last-used save profile** — keep that a spare profile; runs are
started/abandoned constantly and the settings tests exercise unlock/relock state.

## Groups

Scenarios live in one subdirectory per group; each JSON carries `name`, `group`,
`description`, and one of `steps`, `checks`, or `sweep`.

| Group | Covers |
|---|---|
| `cards/` | card/power behavior under seeded combat — costs, effects, once-per-turn caps, token generation, **base + upgraded** numbers, and damage mechanics via a fixed-HP-normalized enemy (Sepsis +50%, Overflow AoE, Virulence poison) |
| `sweeps/` | whole-set crash smoke: play **every** pool card, add all relics, use all potions — any exception (unwrapped to its real type) or player death fails, naming the entity |
| `ancients/` | each custom Ancient (Neow/Darv/Architect): options render with no raw `LocString` keys and the on-screen dialogue shows localized text; plus a general `dialogue_completeness` check that derives every ancient's expected dialogue from `ancients.json` and verifies all 9 ancients' lines register + render (auto-adapts as dialogue changes) |
| `rest/` | WeatheredKit's custom **Brew** rest-site option (right label, removes a deck card, potion reward you claim) and the relic's +3 HP heal on potion use |
| `shop/` | the potion-selling mechanic: Sell button + price on sellables, gold delta, and Foul Potion keeping its base-game Throw behavior |
| `settings/` | the mod's config panel + timeline metaprogression: Unlock All / Reset Unlocks (`[Config]` log counts); the Enable Epochs toggle hides our epochs yet the Timeline still opens cleanly; and the full **progressive unlock** — from a fresh timeline only Alchemist1 is visible (locked), revealing it exposes 2-7, and revealing each of 2-7 unlocks exactly its cards/relics/potions (`epoch_progression`, via `set_epoch`/`get_epoch_state`) |
| `compendium/` | model-level data that drives the Card Library: the Alchemist pool matches `cards.csv` (count + names), tokens in `TokenCardPool`, relic/potion pools complete, and **every entity's rendered title/description has no raw keys, unresolved braces, or canonical-render exceptions** |

A separate offline check, `scripts/dev.sh lint` (`scripts/lint_sync.py`), statically
enforces the three-way rule — every `cards.csv` row ↔ card class ↔ loc keys, plus a
conservative numeric cross-check — with no game needed.

## Scenario formats

**`steps` scenarios** run through `sts2mcp.test_runner` (combat-oriented: `play_card`,
`end_turn`, `console`, assertions on hp/energy/block/powers). Assert in a trailing
`noop` step with `delay: 1` — effects resolve across frames.

**`checks` scenarios** run in `run_suite.py`'s checks engine (the preferred format for
new tests). Each item is `{"do": …, "expect": …, "timeout": s, "note": …}`; expectations
**poll until true** (no fixed sleeps), so they pass the moment the game settles — this is
what keeps the suite fast as it grows.

`do` actions:
- combat: `play_card` (`"CardClassName"`, optional `"target"` enemy index) ·
  `upgrade_card` (`"CardClassName"`) · `end_turn` · `set_enemy_hp`
  (`{enemy, hp}` — normalizes an enemy *downward* to a fixed HP so damage asserts are
  roster-independent)
- potions: `use_potion_ui` (`slot` — drives the belt popup; the bridge's `use_potion`
  reports success but **no-ops**, so always use this) · `discard_potion`
- rooms/rewards: `advance_ancient` (proceed an open ancient event) · `remove_deck_card`
  (`slot` — select + preview-confirm a deck-removal screen) · `reward_select` (`index`)
- console/bridge: `console` · `bridge` (raw method) · `menu` (navigate_menu target)
- UI: `click` (ForceClick a node path) · `click_method` (`"path|Method"`) ·
  `click_label` (`{root, label}` — click a child button by its Label text) ·
  `find_click` (`{pattern, contains, child_class}` — for Godot `@`-generated paths)
- misc: `snapshot: "gold" | "hp"` · `sleep`

`expect` keys:
- player: `hp` · `hp_gain_gte` (vs `snapshot: "hp"`) · `energy` · `block` · `hand_size` ·
  `hand_contains` (card class in hand) · `power` (`{name, amount}`) · `powers` (list) ·
  `has_power` · `gold` / `gold_delta` · `potion_count` · `deck_count`
- enemy: `enemy_hp` (`{enemy, hp}`) · `any_enemy_hp` (some alive enemy at this HP —
  index-independent, for AoE that reindexes the roster)
- screen/events: `screen` / `screen_contains` · `actions_labels_exclude` /
  `actions_label_contains` / `actions_count_gte` · `rest_option` (`{root, label}`)
- content: `node_text` (`{path, contains/excludes}`) · `exceptions_clean` ·
  `game_log_contains` · `pool_contains` / `pool_count` / `pool_matches_csv` ·
  `loc_render_clean` · `ancient_dialogues` · `dialogue_on_screen` ·
  `dialogue_loc_complete` (all ancients' loc vs registered dialogue)
- timeline: `epoch_state` (`{prefix, epochs/cards/relics/potions: {model_id: {field: expected}}}`
  — epoch fields `state`/`visible`/`revealed`, content fields `unlocked`/`discovered`; reads the
  bridge's `get_epoch_state`). Drive reveals with `do: {bridge: "set_epoch", params: {id, state}}`
  (`state` a `EpochState` name, or `"remove"`; on `"Revealed"` it also slots the epoch's expansion
  children, mirroring the in-game reveal).

**`sweep` scenarios** (`"sweep": "cards" | "relics" | "potions"`) are python-implemented
crash passes: each entity is exercised from a fresh combat, exceptions are unwrapped to
their real type and attributed to a card only when the stack references the `Alchemist`
namespace (base-game/harness noise is reported separately), and a player death is caught
and named. (This is how the suite found the Harvest death-cleanup crash.)

## Bridge quirks these scenarios encode (learned the hard way)

- **Fresh-process rule**: abandoning a run mid-combat poisons combat init for the rest
  of the process. The runner never does this (`die` in combat instead) and auto-restarts
  the game when a reset fails, so you usually don't have to care.
- **Custom entities need full model IDs** (`card ALCHEMIST-SEPSIS`), not class/display
  names — bare names silently no-op. Injected cards append to the **end** of hand
  (index 5 after a 5-card opening hand). Discover IDs via console `dump` + the game log.
- **console `power`/`damage`/`block` target-index is offset**: `0` = player,
  `1` = first enemy … (enemy array index + 1); `play_card`'s `target_index` is 0-based.
- **Enemy rosters are not seed-stable** (`fight SLIMES_WEAK` re-rolls HP each run) —
  don't assert absolute enemy HP; assert player state, block, or pools.
- **Enemy power amounts aren't assertable** — only `enemy_N_hp`/block + player powers.
- **Potion belt UI slots don't re-shuffle** after a potion is consumed; slot-0's holder
  node is `…/PotionHolders/PotionHolder` and popups open via the holder's
  `OpenPotionPopup` method (the belt isn't ForceClick-able).
- The bridge's screen-context can wedge on some base-game popups (disposed object) —
  the runner's health check handles it with a restart.
