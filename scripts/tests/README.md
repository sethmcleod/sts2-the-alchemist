# Regression test suite

Seeded test scenarios find breakage when you change the mod. They cover cards, mechanics,
localization, registration, and the mod's own UI. **You do not need AI tools.** The suite
runs with plain Python against the live game. The suite also controls the game process. It
starts the game if the game does not run. It restarts the game if the game crashes or fails.

## How to run the suite

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

The commands `scripts/dev.sh game-start | game-stop | game-restart` control the game process
directly. Steam must run first, because the game starts through the `steam://` protocol. The
command `scripts/dev.sh doctor` checks every prerequisite.

**How to select what to run.** The `--changed` option maps your changed files to the groups
that they can break. The map is `_CHANGE_MAP` in `run_suite.py`. The option runs only those
groups. It prints each path and the groups that it selected.

The `--changed` option is a convenience for the inner loop. It is not a safety net. When it
is not sure, it runs more tests and not fewer:

- An unmapped path runs the *whole* suite. Examples are the character model, this harness,
  and a new directory.
- A change to only docs or only art runs no tests.
- The command `scripts/dev.sh release` always runs every group.

When you add a subsystem, add the subsystem to `_CHANGE_MAP`. If you are not sure, map the
subsystem broadly. A map that is too narrow can miss a regression. A map that is too broad
costs only a few seconds.

**Speed and batching.** Three mechanisms keep the suite quick. Learn all three, because a
slow run usually points at one of them.

- **Batching** saves the most time. A menu round trip costs about 3.2s. A new run costs
  about 3.2s more. There are two batch modes (`_batch_mode`):
  - `"combat"`. The suite **infers** this mode for `checks` scenarios with a setup of only
    a `SLIMES_WEAK` fight. These scenarios share one run. The suite starts a fresh `fight`
    between them, so no state carries over.
  - `"run"`. You **opt in** to this mode with `"batch": "run"`. Use it for scenarios that
    reach their own room by console. Such a scenario must not depend on the state of the
    run. It must also leave no state behind. The ancients qualify (`console ancient X`).
    Their time decreased from about 7s each to about 1.4s.

  The `"run"` mode is opt-in for a reason. A shared run also shares every change that the
  previous scenario made to it. The suite once inferred this mode from the shape of the
  setup. That inference broke the shop tests, because they then read each other's potions
  (`potion_count == 1 (actual: 2)`). Claim the mode only for fully self-contained scenarios.
  To opt out completely, set `"batch": false`. A seed-sensitive scenario must opt out,
  because batched neighbours share one run and one seed.
- **`FastMode = Instant`** makes the game's `Cmd.Wait` and `CustomScaledWait` return
  immediately. **`Engine.TimeScale`** (`--speed`, default 3) then shortens what is left.
  Both settings live in the game process. Therefore `apply_perf()` sets them again after
  *every* restart. A restart that skips them makes the rest of the run twice as long, and
  gives no message.
- **Poll intervals.** A bridge call resolves in about one 60fps frame. Therefore the waiters
  poll at 0.1s and not at 0.5s. If a run pauses in whole half-seconds, some code still polls
  at the old interval.

These two changes were measured and give no improvement. Do not try them again:

- An increase of `--speed` above 3 has no effect. The `fight` transition of about 270ms is
  scene init, and `TimeScale` does not change scene init.
- An increase of the frame rate has no effect. An uncapped FPS with vsync off kept the same
  per-call floor of about 16ms. An uncapped FPS with the window focused was *worse*.

That floor is one main-thread dispatch. It is the practical limit on the speed at which a
scripted run can drive the game.

`cards_sweep` (all 86 cards, about 48s) takes most of the time that is left. About 86 × 270ms
of that time is combat init for the fresh fight of each card. That fresh fight gives the
isolation that lets a failure name one card. Use `--changed` or `--group` for your inner
loop. Run the whole suite before a release.

This suite is **stateful** and it uses the **live game**. Each group passes in isolation. A
very long full run can sometimes fail on a rare UI-timing problem, for example the shop
potion popup. Run that group again.

Prerequisites:

- The game is installed.
- The `mcptest` and `godotexplorer` bridge mods are current (`scripts/dev.sh bridge`). The
  compendium group needs a bridge from a checkout that has the `get_compendium` RPC.
- The Alchemist mod is published.
- Python 3.10 or later is available.

The suite uses no third-party packages. It drives the bridge over TCP with the stdlib.
Therefore any interpreter of version 3.10 or later works. The simplest way to get one is
[uv](https://astral.sh/uv), and the sts2-modding-mcp toolkit uses it. Install uv, and then
`scripts/dev.sh` supplies Python for you automatically. The script still prefers a system
`python3` that is already on PATH.

> [!CAUTION]
> The game starts in the **last-used save profile**, so keep that profile a spare profile.
> The suite starts and abandons runs continuously. The settings tests also change the unlock
> and relock state. A save profile that you care about will lose its progression.

## Groups

Each group has one subdirectory of scenarios. Each JSON file has `name`, `group`,
`description`, and one of `steps`, `checks`, or `sweep`.

| Group | Covers |
|---|---|
| `cards/` | Card and power behavior in seeded combat: costs, effects, once-per-turn caps, token generation, and **base and upgraded** numbers. Also damage mechanics against an enemy that is normalized to a fixed HP (Sepsis +50%, Overflow AoE, Virulence poison) |
| `sweeps/` | Crash smoke test of the whole set. It plays **every** pool card. It adds all relics. It uses all potions. Any exception (unwrapped to its real type) or player death fails the test and names the entity |
| `ancients/` | **Each of the 9 Ancients** (Darv/Neow/Nonupeipe/Orobas/Pael/Tanx/Tezcatara/Vakuu/Architect) has its own scenario. The options render with no raw `LocString` keys. All 3 of its visit conversations register and render (`min_dialogues: 3`). The 8 standard ancients also use `walk_dialogue`. It clicks through **every** line of the conversation on screen. It asserts that each line renders and that the sequence reaches its last line. The Architect's finale uses a combat layout, so the registry checks cover it. A general `dialogue_completeness` check derives each ancient's expected **conversation count and line count** from `ancients.json`. It then verifies that the live registered dialogue matches and renders. The check adapts automatically when the dialogue changes |
| `rest/` | WeatheredKit's custom **Brew** rest-site option: the correct label, the removal of a deck card, and a potion reward that you claim. Also the relic's +3 HP heal on potion use |
| `shop/` | The mechanic to sell potions: the Sell button and the price on sellable items, the gold delta, and the Foul Potion, which keeps its base-game Throw behavior |
| `settings/` | The mod's config panel and the Timeline metaprogression. Unlock All and Reset Unlocks (`[Config]` log counts). The Enable Epochs toggle hides the mod's epochs, but the Timeline still opens correctly. The full **progressive unlock**: from a fresh timeline only Alchemist1 is visible, and it is locked. A reveal of Alchemist1 exposes epochs 2-7. A reveal of each of epochs 2-7 unlocks exactly its own cards, relics, and potions (`epoch_progression`, via `set_epoch` and `get_epoch_state`) |
| `compendium/` | Model-level data that drives the Card Library. The Alchemist pool matches `cards.csv` (count and names). The tokens are in `TokenCardPool`. The relic and potion pools are complete. **The rendered title and description of every entity has no raw keys, no unresolved braces, and no canonical-render exceptions** |

A separate offline check is `scripts/dev.sh lint` (`scripts/lint_sync.py`). It enforces the
three-way rule statically: every `cards.csv` row ↔ card class ↔ loc keys, plus a
conservative numeric cross-check. It does not need the game.

## Scenario formats

**`steps` scenarios** run through `sts2mcp.test_runner`. The format is combat-oriented:
`play_card`, `end_turn`, `console`, and assertions on hp, energy, block, and powers. Put
your assertions in a final `noop` step with `delay: 1`. Effects resolve across more than
one frame.

**`checks` scenarios** run in the checks engine of `run_suite.py`. Use this format for new
tests. Each item is `{"do": …, "expect": …, "timeout": s, "note": …}`. Expectations **poll
until true**, with no fixed sleeps. Therefore they pass as soon as the game settles. This
behavior keeps the suite fast as it grows.

`do` actions:
- combat: `play_card` (`"CardClassName"`, with an optional `"target"` enemy index) ·
  `upgrade_card` (`"CardClassName"`) · `end_turn` · `set_enemy_hp`
  (`{enemy, hp}`, sets an enemy *down* to a fixed HP, so damage assertions do not depend on
  the roster) · `select_hand_cards` (`"CardClassName"` or a list, answers an in-combat
  hand-selection prompt such as Infuse, then confirms it)
- potions: `use_potion_ui` (`slot`, drives the belt popup; the bridge's `use_potion` reports
  success but has **no effect**, so always use `use_potion_ui`) · `discard_potion`
- rooms and rewards: `advance_ancient` (proceeds an open ancient event) · `walk_dialogue`
  (clicks the invisible "next" hitbox through **every** line of an open ancient's dialogue.
  It asserts that each line renders. It asserts that the sequence reaches its last line. It
  waits for the deferred line-node adds) · `remove_deck_card` (`slot`, selects a card on a
  deck-removal screen, then confirms it in the preview) · `reward_select` (`index`)
- console and bridge: `console` · `bridge` (a raw method) · `menu` (a navigate_menu target)
- timeline: `reveal_timeline` (`{id?, timeout?}`, reveals epochs through the real Timeline UI.
  The Timeline screen must already be open. It clicks the obtained tile, closes the inspect
  screen, and confirms each queued unlock screen, and it waits out the animations between the
  steps. It fails the check if an epoch has no tile to click)
- UI: `click` (ForceClick on a node path) · `click_method` (`"path|Method"`) ·
  `click_label` (`{root, label}`, clicks a child button by its Label text) ·
  `find_click` (`{pattern, contains, child_class}`, for the paths that Godot generates
  with `@`)
- misc: `snapshot: "gold" | "hp"` · `sleep`

`expect` keys:
- player: `hp` · `hp_gain_gte` (against `snapshot: "hp"`) · `energy` · `block` · `hand_size` ·
  `hand_contains` (a card class in the hand) · `power` (`{name, amount}`) · `powers` (a list) ·
  `has_power` · `gold` / `gold_delta` · `potion_count` · `deck_count`
- enemy: `enemy_hp` (`{enemy, hp}`) · `any_enemy_hp` (any live enemy at this HP; it does not
  depend on the index, for AoE that changes the index of the roster)
- screen and events: `screen` / `screen_contains` · `actions_labels_exclude` /
  `actions_label_contains` / `actions_count_gte` · `rest_option` (`{root, label}`)
- content: `node_text` (`{path, contains/excludes}`) · `exceptions_clean` ·
  `game_log_contains` · `pool_contains` / `pool_count` / `pool_matches_csv` ·
  `loc_render_clean` · `ancient_dialogues` · `dialogue_on_screen` ·
  `dialogue_loc_complete` (the loc of all ancients against the registered dialogue)
- timeline: `epoch_state` (`{prefix, epochs/cards/relics/potions: {model_id: {field: expected}}}`).
  The epoch fields are `state`/`visible`/`revealed`/`slot_count`/`slot_state`. The content
  fields are `unlocked`/`discovered`. This key reads the bridge's `get_epoch_state`.

  There are two ways to make a reveal, and they test different things.
  `do: {bridge: "set_epoch", params: {id, state}}` writes the save state directly. The `state`
  value is an `EpochState` name, or `"remove"`. For `"Revealed"`, the bridge also slots the
  expansion children of the epoch. This is fast and deterministic, so use it for setup.
  But it never opens the Timeline, so it does not run the epoch's `QueueUnlocks` and it does
  not reach `AddEpochSlots`. `do: {reveal_timeline: {id}}` drives the real UI instead and does
  run both. See `epoch_progression.json` for the first form and `epoch_reveal_ui.json` for
  the second.

  `slot_count` is the number of live tiles that carry an epoch id. It is `0` while the
  Timeline screen is closed, so assert it only after `do: {menu: "timeline"}`. A count above
  `1` means the epoch was drawn twice, because `AddEpochSlots` has no dedup.

**`sweep` scenarios** (`"sweep": "cards" | "relics" | "potions"`) are crash passes written
in Python:

- The sweep exercises each entity from a fresh combat.
- The sweep unwraps each exception to its real type.
- The sweep attributes an exception to a card only when the stack references the `Alchemist`
  namespace. It reports base-game and harness exceptions separately.
- The sweep catches a player death and names the entity.

The suite found the Harvest death-cleanup crash with this format.

## Bridge quirks these scenarios encode

- **Fresh-process rule**: if you abandon a run during combat, combat init stays broken for
  the rest of the process. The runner never does this. It uses `die` in combat instead. It
  also restarts the game automatically when a reset fails. Therefore you usually do not need
  to think about this rule.
- **Custom entities need full model IDs** (`card ALCHEMIST-SEPSIS`), not class names or
  display names. A bare name does nothing and gives no message. An injected card goes to the
  **end** of the hand (index 5 after a first hand of 5 cards). To find the IDs, use the
  console `dump` command and the game log.
- **The target index of the console `power`/`damage`/`block` commands has an offset**:
  `0` = the player, `1` = the first enemy, and so on (the enemy array index plus 1). The
  `target_index` of `play_card` starts at 0.
- **A hand-selection prompt uses a different index space than the hand.** The bridge's
  `combat_select_card` addresses `NPlayerHand.ActiveHolders`, the visual fan. The order of
  that list does *not* follow the logical hand from `get_combat_state`. When you select a
  card, the bridge removes its holder from that list. Therefore a hand index selects the
  *wrong card*, with no message and in the same way each time. Always select a card by its
  name. The `select_hand_cards` action does this with the bridge's `card_name` parameter.
  Never select a card by its index.
- **Enemy rosters are not seed-stable.** The `fight SLIMES_WEAK` command rolls new HP for
  each run. Do not assert an absolute enemy HP. Assert the player state, the block, or the
  pools.
- **You cannot assert enemy power amounts.** You can assert only `enemy_N_hp`, block, and
  player powers.
- **The potion belt UI slots do not move** after the player consumes a potion. The holder
  node of slot 0 is `…/PotionHolders/PotionHolder`. To open a popup, call the holder's
  `OpenPotionPopup` method. ForceClick does not work on the belt.
- The bridge's screen context can fail on some base-game popups (a disposed object). The
  runner's health check corrects this with a restart.
- **An `ObtainedNoSlot` epoch cannot be revealed through the UI.** That state is the only one
  that `NTimelineScreen.InitScreen` draws no tile for. Therefore there is nothing to click,
  and `reveal_timeline` fails the check instead of a wait that never ends. Promote the epoch
  first with `do: {bridge: "set_epoch", params: {id, state: "Obtained"}}`, or reload the save,
  because that runs `FixMissingSlots`.
- **Set the epoch state before you open the Timeline.** `InitScreen` reads the save state once,
  when the screen opens. A `set_epoch` call while the screen is open does not change the tiles.
  Go back to the main menu and open the Timeline again.
