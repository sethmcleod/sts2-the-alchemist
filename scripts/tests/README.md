# Regression test suite

Seeded test scenarios that catch breakage when changing the mod: cards, mechanics,
localization, registration, and the mod's own UI. **No AI tooling required.** The suite
runs with plain Python against the live game, and manages the game process itself
(starts it if it's not running, restarts it if it crashes or wedges).

## Running the suite

```sh
scripts/setup.sh                    # once: clones tooling, installs the bridge mods
scripts/dev.sh test                 # run everything
scripts/dev.sh test --changed       # only the groups your uncommitted work can affect
scripts/dev.sh test --changed-since main   # ...or everything that differs from a ref
scripts/dev.sh test --group shop    # one group
scripts/dev.sh test sepsis          # scenarios whose filename contains "sepsis"
scripts/dev.sh test --fresh         # force a game restart first (suspect state)
scripts/dev.sh test --speed 1       # watchable speed (default 3; >3 glitches the hand animation cosmetically)
scripts/dev.sh test --fast-mode Fast  # watchable animations (default Instant, which skips them)
scripts/dev.sh test --no-batch      # disable run-batching (see below) for debugging
```

Also: `scripts/dev.sh game-start | game-stop | game-restart` manage the game process
directly (Steam must be running; the game launches via the `steam://` protocol).
`scripts/dev.sh doctor` checks every prerequisite.

**Picking what to run.** `--changed` maps your changed files to the groups they can plausibly
break (see `_CHANGE_MAP` in `run_suite.py`) and runs only those, printing each path and the
groups it selected. It is a convenience for the inner loop, not a safety net, so it fails
safe: anything unmapped (the character model, this harness, a new directory) runs the *whole*
suite, and a docs- or art-only change runs nothing. `scripts/dev.sh release` always runs
everything regardless. When you add a subsystem, add it to `_CHANGE_MAP`; when unsure, map it
broadly, since the cost of being wrong is a missed regression and the cost of being broad is
a few seconds.

**Speed / batching.** Three things keep the suite quick, and all three are worth knowing about
if a run suddenly crawls:

- **Batching** is the big one, because a menu round-trip costs ~3.2s and starting a run another
  ~3.2s. Two modes (`_batch_mode`):
  - `"combat"`, **inferred** for `checks` scenarios whose setup is just a `SLIMES_WEAK` fight.
    They share a run and get a fresh `fight` between them, so nothing carries over.
  - `"run"`, **opt in** with `"batch": "run"`, for scenarios that reach their own room by
    console and neither care what the run looks like nor leave anything behind. The ancients
    qualify (`console ancient X`) and went from ~7s each to ~1.4s.

  `"run"` is opt-in for a reason: sharing a run shares everything the previous scenario did to
  it. Inferring it from setup shape looked fine and quietly broke the shop tests, which read
  each other's potions (`potion_count == 1 (actual: 2)`). Only claim it for genuinely
  self-contained scenarios. `"batch": false` opts out entirely; anything seed-sensitive must,
  since batched neighbours share one run and one seed.
- **`FastMode = Instant`** makes the game's `Cmd.Wait`/`CustomScaledWait` return without
  waiting at all, and **`Engine.TimeScale`** (`--speed`, default 3) shortens what's left. Both
  live in the game process, so `apply_perf()` re-applies them after *every* restart; a restart
  that skips them silently doubles the remaining run.
- **Poll intervals.** Bridge calls resolve in about one 60fps frame, so the waiters poll at
  0.1s rather than 0.5s. If you see a run pause in whole half-seconds, something is polling at
  the old interval.

Measured dead ends, so you don't re-try them: raising `--speed` past 3 does nothing (the
~270ms `fight` transition is scene init, which `TimeScale` doesn't touch), and neither does
raising the frame rate (uncapping FPS and disabling vsync left the ~16ms per-call floor
unchanged, and focused+uncapped was *worse*). That floor is one main-thread dispatch and is
the practical limit on how fast a scripted run can drive the game.

`cards_sweep` (all 86 cards, ~48s) dominates what's left: 86 × ~270ms of that is combat init
for the per-card fresh fight, which buys the isolation that lets a failure name one card. Use
`--changed` or `--group` while iterating and run the whole suite before a release.

This is a **stateful live-game** suite. Each group passes in isolation, and a very long
full run may occasionally hit a rare UI-timing flake (e.g. the shop potion popup). Just
re-run that group.

Prerequisites: game installed, `mcptest` + `godotexplorer` bridge mods current
(`scripts/dev.sh bridge`, and note the compendium group needs a bridge built from a checkout
that has the `get_compendium` RPC), Alchemist mod published, and Python ≥ 3.10. The
suite pulls in no third-party packages (it drives the bridge over TCP with the stdlib),
so any 3.10+ interpreter works. The easiest way to get one, and what the sts2-modding-mcp
toolkit uses, is [uv](https://astral.sh/uv): install it and `scripts/dev.sh` provisions
Python for you automatically (it still prefers a system `python3` if one's already on PATH).

> [!CAUTION]
> The game boots into the **last-used save profile**, so keep that a spare profile. The
> suite starts/abandons runs constantly and the settings tests exercise unlock/relock
> state, so pointing it at a save you care about will churn its progression.

## Groups

Scenarios live in one subdirectory per group; each JSON carries `name`, `group`,
`description`, and one of `steps`, `checks`, or `sweep`.

| Group | Covers |
|---|---|
| `cards/` | card/power behavior under seeded combat: costs, effects, once-per-turn caps, token generation, **base + upgraded** numbers, and damage mechanics via a fixed-HP-normalized enemy (Sepsis +50%, Overflow AoE, Virulence poison) |
| `sweeps/` | whole-set crash smoke: play **every** pool card, add all relics, use all potions. Any exception (unwrapped to its real type) or player death fails, naming the entity |
| `ancients/` | **each of the 9 Ancients** (Darv/Neow/Nonupeipe/Orobas/Pael/Tanx/Tezcatara/Vakuu/Architect) gets its own scenario: options render with no raw `LocString` keys, and all 3 of its visit conversations register + render (`min_dialogues: 3`). The 8 standard ancients additionally `walk_dialogue`, clicking through **every line** of the shown conversation on screen, asserting each renders and the sequence reaches its last line (the Architect's finale uses a combat layout, so it's covered by the registry checks). Plus a general `dialogue_completeness` check that derives every ancient's expected **conversation count and line count** from `ancients.json` and verifies the live registered dialogue matches and renders (auto-adapts as dialogue changes) |
| `rest/` | WeatheredKit's custom **Brew** rest-site option (right label, removes a deck card, potion reward you claim) and the relic's +3 HP heal on potion use |
| `shop/` | the potion-selling mechanic: Sell button + price on sellables, gold delta, and Foul Potion keeping its base-game Throw behavior |
| `settings/` | the mod's config panel + timeline metaprogression: Unlock All / Reset Unlocks (`[Config]` log counts); the Enable Epochs toggle hides our epochs yet the Timeline still opens cleanly; and the full **progressive unlock**, where from a fresh timeline only Alchemist1 is visible (locked), revealing it exposes 2-7, and revealing each of 2-7 unlocks exactly its cards/relics/potions (`epoch_progression`, via `set_epoch`/`get_epoch_state`) |
| `compendium/` | model-level data that drives the Card Library: the Alchemist pool matches `cards.csv` (count + names), tokens in `TokenCardPool`, relic/potion pools complete, and **every entity's rendered title/description has no raw keys, unresolved braces, or canonical-render exceptions** |

A separate offline check, `scripts/dev.sh lint` (`scripts/lint_sync.py`), statically
enforces the three-way rule (every `cards.csv` row ↔ card class ↔ loc keys, plus a
conservative numeric cross-check) with no game needed.

## Scenario formats

**`steps` scenarios** run through `sts2mcp.test_runner` (combat-oriented: `play_card`,
`end_turn`, `console`, assertions on hp/energy/block/powers). Assert in a trailing
`noop` step with `delay: 1`, since effects resolve across frames.

**`checks` scenarios** run in `run_suite.py`'s checks engine (the preferred format for
new tests). Each item is `{"do": …, "expect": …, "timeout": s, "note": …}`; expectations
**poll until true** (no fixed sleeps), so they pass the moment the game settles. That's
what keeps the suite fast as it grows.

`do` actions:
- combat: `play_card` (`"CardClassName"`, optional `"target"` enemy index) ·
  `upgrade_card` (`"CardClassName"`) · `end_turn` · `set_enemy_hp`
  (`{enemy, hp}`, normalizes an enemy *downward* to a fixed HP so damage asserts are
  roster-independent) · `select_hand_cards` (`"CardClassName"` or a list, answers an
  in-combat hand-selection prompt such as Infuse, then confirms)
- potions: `use_potion_ui` (`slot`, drives the belt popup; the bridge's `use_potion`
  reports success but **no-ops**, so always use this) · `discard_potion`
- rooms/rewards: `advance_ancient` (proceed an open ancient event) · `walk_dialogue`
  (click the invisible "next" hitbox through **every** line of an open ancient's dialogue,
  asserting each renders and the sequence reaches its last line, waiting out the deferred
  line-node adds) · `remove_deck_card` (`slot`, select + preview-confirm a deck-removal
  screen) · `reward_select` (`index`)
- console/bridge: `console` · `bridge` (raw method) · `menu` (navigate_menu target)
- UI: `click` (ForceClick a node path) · `click_method` (`"path|Method"`) ·
  `click_label` (`{root, label}`, click a child button by its Label text) ·
  `find_click` (`{pattern, contains, child_class}`, for Godot `@`-generated paths)
- misc: `snapshot: "gold" | "hp"` · `sleep`

`expect` keys:
- player: `hp` · `hp_gain_gte` (vs `snapshot: "hp"`) · `energy` · `block` · `hand_size` ·
  `hand_contains` (card class in hand) · `power` (`{name, amount}`) · `powers` (list) ·
  `has_power` · `gold` / `gold_delta` · `potion_count` · `deck_count`
- enemy: `enemy_hp` (`{enemy, hp}`) · `any_enemy_hp` (some alive enemy at this HP,
  index-independent, for AoE that reindexes the roster)
- screen/events: `screen` / `screen_contains` · `actions_labels_exclude` /
  `actions_label_contains` / `actions_count_gte` · `rest_option` (`{root, label}`)
- content: `node_text` (`{path, contains/excludes}`) · `exceptions_clean` ·
  `game_log_contains` · `pool_contains` / `pool_count` / `pool_matches_csv` ·
  `loc_render_clean` · `ancient_dialogues` · `dialogue_on_screen` ·
  `dialogue_loc_complete` (all ancients' loc vs registered dialogue)
- timeline: `epoch_state` (`{prefix, epochs/cards/relics/potions: {model_id: {field: expected}}}`,
  where epoch fields are `state`/`visible`/`revealed` and content fields are `unlocked`/`discovered`; reads the
  bridge's `get_epoch_state`). Drive reveals with `do: {bridge: "set_epoch", params: {id, state}}`
  (`state` a `EpochState` name, or `"remove"`; on `"Revealed"` it also slots the epoch's expansion
  children, mirroring the in-game reveal).

**`sweep` scenarios** (`"sweep": "cards" | "relics" | "potions"`) are python-implemented
crash passes: each entity is exercised from a fresh combat, exceptions are unwrapped to
their real type and attributed to a card only when the stack references the `Alchemist`
namespace (base-game/harness noise is reported separately), and a player death is caught
and named. This is how the suite found the Harvest death-cleanup crash.

## Bridge quirks these scenarios encode

- **Fresh-process rule**: abandoning a run mid-combat poisons combat init for the rest
  of the process. The runner never does this (`die` in combat instead) and auto-restarts
  the game when a reset fails, so you usually don't have to care.
- **Custom entities need full model IDs** (`card ALCHEMIST-SEPSIS`), not class/display
  names, since bare names silently no-op. Injected cards append to the **end** of hand
  (index 5 after a 5-card opening hand). Discover IDs via console `dump` + the game log.
- **console `power`/`damage`/`block` target-index is offset**: `0` = player,
  `1` = first enemy … (enemy array index + 1); `play_card`'s `target_index` is 0-based.
- **Hand-selection prompts use a different index space than the hand.** The bridge's
  `combat_select_card` addresses `NPlayerHand.ActiveHolders` (the visual fan), whose order
  does *not* track the logical hand from `get_combat_state`, and selecting a card pulls its
  holder out of that list. Feeding it a hand index therefore picks the *wrong card*, silently
  and repeatably. Always select by name (`select_hand_cards` does, via the bridge's
  `card_name` param), never by index.
- **Enemy rosters are not seed-stable** (`fight SLIMES_WEAK` re-rolls HP each run), so
  don't assert absolute enemy HP. Assert player state, block, or pools.
- **Enemy power amounts aren't assertable**, only `enemy_N_hp`/block + player powers.
- **Potion belt UI slots don't re-shuffle** after a potion is consumed. Slot-0's holder
  node is `…/PotionHolders/PotionHolder` and popups open via the holder's
  `OpenPotionPopup` method (the belt isn't ForceClick-able).
- The bridge's screen-context can wedge on some base-game popups (disposed object), and
  the runner's health check handles it with a restart.
