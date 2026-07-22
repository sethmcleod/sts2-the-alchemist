# Backlog

This file lists improvements that are not scheduled yet. Balance passes and the character
artwork have their own records, and they are not in this file.

The scope is any work that this repo can do. Two related documents have their own lists:

- Fixes that must go into BaseLib are in [baselib-improvements.md](baselib-improvements.md).
- Known problems that already have a workaround are in [troubleshooting.md](troubleshooting.md).

The [sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) toolkit is ours to fix
directly. Thus this file also lists the gaps in that toolkit, and it identifies them as
toolkit items.

Each entry records its evidence. Thus you can examine an old entry again, and a discussion
about the entry is not necessary. No entry in this file is a commitment. The sequence of the
items in a section shows an approximate priority.

---

## Content and presentation

### 1. Per-card SFX and VFX

**Status:** idea, unblocked, incremental
**Evidence:** 0 of 99 card files call `WithHitFx`, `WithAttackerFx` or `VfxCmd`. The only
`SfxCmd.Play` is a merchant line in [PotionSellPatches.cs](../AlchemistCode/Patches/PotionSellPatches.cs)

225 of 593 base-game card files (38%) attach a sound or a visual effect when the player plays
the card. The Alchemist cards attach none. The engine gives only one generic sound for card
*movement* (`SfxCmd.PlayCardSwooshSfx`). Each card must set its own impact effect and its own
cast effect, and the default value is null.

This work needs no new assets:

- `MegaCrit.Sts2.Core.Audio/FmodSfx.cs` has 45 event constants (`buff`, `debuff`, `heal`,
  `cardImpactIntoSingle`).
- `VfxCmd` has about 27 path constants.
- There are 167 `N*Vfx` classes. One of them is `NPoisonImpactVfx`, which base poison cards
  such as `BouncingFlask` already use.

The poison cards alone need about one hour of work. They give most of the improvement that a
player notices. You can start this task at any card, and you can stop at any card.

### 2. The rest of the character is still the Ironclad

**Status:** blocked on artwork
**Evidence:** [Alchemist.cs](../AlchemistCode/Character/Alchemist.cs) does not override
`PlaceholderID`, which `BaseLib.Abstracts.PlaceholderCharacterModel` defines as `"ironclad"`

The audio is correct now. `CustomAttackSfx`, `CustomCastSfx` and `CustomDeathSfx` point at the
Silent and the Necrobinder. All the other assets that `PlaceholderID` controls are still
Ironclad assets, and each one needs a new asset:

- `CustomVisualPath` (the creature)
- `CustomTrailPath` (the trail of a card play)
- `CustomRestSiteAnimPath`
- `CustomMerchantAnimPath`
- `CustomArm*TexturePath` (the multiplayer rock-paper-scissors hands)

The hands are important, because the mod contains 4 co-op cards. The character also inherits
two more sfx hooks: `CharacterSelectSfx` and `CharacterTransitionSfx`.

When the real art exists, an override of `PlaceholderID` can be simpler than an override of
each path. But an override of `PlaceholderID` also changes the sfx. Keep the audio overrides.

### 3. A custom FMOD bank for new audio

**Status:** idea, needs investigation
**Evidence:** the game loads banks from a hardcoded `bank_paths` array on an `FmodBankLoader`
node in the main `.tscn` of the game. `res://addons/fmod/` is a GDExtension

This is the full fix for the audio in item #1 and item #2. In principle, a mod can make its
own `FmodBankLoader` that points at its own bank. But that needs work in FMOD Studio, and
nobody has verified the behavior of a GUID collision.

Examine this less expensive alternative first. The base game also has sounds that do not use
FMOD: `NDebugAudioManager` plays plain Godot `AudioStream` files from `res://debug_audio/`.
81 card files refer to a `.mp3` file, and the third `tmpSfx` parameter of `WithHitFx` takes
such a file. A mod pck can add `res://` paths, and `NDebugAudioManager.Instance.Play` is
public.

### 4. A custom map event

**Status:** idea
**Evidence:** `AlchemistCode/` contains no `EventModel` or `ConstructedEventModel`

The README says that the character is part of the timeline of the world. An event shows this
to the players who do not read the epoch text. The mod has dialogue for 8 base-game ancients,
but it has no room of its own.

### 5. Other content types are absent

**Status:** idea
**Evidence:** the repo contains no `MonsterModel`, `EncounterModel`, `OrbModel`,
`ModifierModel` or `AncientModel`

The mod has no custom monsters, encounters, orbs, run modifiers, or custom ancients. Each one
is a new subsystem with its own registration interface. Thus each one is a project, not a
task. This entry is here for completeness. It does not show that any of these items is
necessary.

### 6. Non-English localization

**Status:** idea, large but simple
**Evidence:** `Alchemist/localization/` contains only `eng/`

The 13 files have a good structure, and the character keys have full parity with the base
game. Thus this task is translation work, and it does not need a change to the code
structure. Also, the mod has no hardcoded English text for the user in the code.

---

## Tests and tooling

### 7. No test covers the multiplayer cards

**Status:** ready, needs a co-op harness
**Evidence:** `MULTIPLAYER_CARDS = {"Bestow", "Effervesce", "Reflux", "Suffuse"}` at
[run_suite.py:72](../scripts/tests/run_suite.py)

`cards_sweep` excludes the 4 co-op cards with a hardcoded list, and no other test covers them.
Thus they are the content with the least test coverage in the mod. `Bestow` is the most
important of the four. It is the only caller of `Infusion.InfuseRandomFromHand`, and no test
reaches that method.

A test of these cards needs a second player. Thus a test probably must use the multiplayer
test scene of the game (console `multiplayer test`).

### 8. `cards_sweep` accepts unplayable cards without a failure

**Status:** ready, small
**Evidence:** [run_suite.py](../scripts/tests/run_suite.py) prints
`{skipped} unplayable-skipped`, but it never asserts on this count

The sweep counts and prints a card that has `can_play` false at runtime. The sweep does not
fail on that card. Thus a card can become permanently unplayable, and the sweep continues to
pass. Make one of these two changes:

- Assert that the count is 0.
- Assert against a list of the expected cards.

### 9. The rest of the runtime of the suite

**Status:** idea; more work gives a small return. This entry records the approaches that
failed, so that nobody tries them again
**Evidence:** measurement on 2026-07-16. The full suite takes about 197s of wall time, of
which 151s is scenarios. There were 0 restarts

This list shows where the rest of the time goes. It also shows the changes that do not decrease
the time.

- `cards_sweep` takes about 48s. 86 × about 270ms of that time is the `fight` transition for
  each card. A new fight for each card lets a failure identify one card. Reuse of one fight
  for more than one card saves about 20s. But powers then remain from one card to the next
  card, and the suite can name the wrong card for an exception. Do this only with a bisect
  step after a failure.
- About 46s is between the scenarios: `reset_to_menu` (about 3.2s) plus `start_run` (about
  3.2s) for the 11 scenarios that cannot batch. `"batch": "run"` is the control for this time.
  Use it only for scenarios that are fully self-contained (see `scripts/tests/README.md`).
- Each bridge call to the main thread costs about 16ms. The suite makes about 10 of these
  calls for each card. `_hand_index` reads state that its caller read immediately before. A
  combination of these reads can save 5-9s.

Measurements show that these two approaches give no improvement:

- `--speed` above 3 has no effect. The fight transition is scene initialization, not animation
  (266ms at 3x against 272ms at 20x).
- A higher frame rate has no effect. An uncapped frame rate with vsync off kept the 16ms
  minimum. A focused window with an uncapped frame rate was worse at 21ms.

### 10. The bridge cannot see enchantments

**Status:** ready, ours to fix (toolkit)
**Evidence:** there are zero `enchant` matches in `test_mod/Code/BridgeHandler.cs`. Hand cards
serialize `name`, `type`, `energy_cost` and `upgraded` only

The bridge shows no enchantment state. Thus the Infuse tests assert enchantments *by their
effect* (Exalted gives Strength, Fuming adds Foul Vapor). This method is possibly a better
test. But no test can assert Toxic directly. Toxic applies Poison to an enemy, and an
assertion cannot read the power stacks of an enemy. An addition of `enchantment` and
`enchantment_amount` to the hand-card payload closes this gap.

### 11. `list_game_audio` and `list_game_vfx` always return an empty result

**Status:** ready, ours to fix (toolkit)
**Evidence:** `_load_fmod_data()` at `sts2mcp/server.py` looks for `fmod_dump.json` in two
paths. It finds no file in either path, and it returns `{"events": []}` with no message

Each query returns 0 results, but the tool reports "563 FMOD events across 12 banks".
`get_setup_status` reports the ready state, because it never checks this data. The repo
contains no `fmoddumper` mod. This problem has a direct effect on item #1.

Two workarounds are available now:

- `grep -rhoE '"event:/[^"]*"' decompiled` recovers 369 event paths.
- An extracted copy of the game (see `extract_game_assets`) has the actual banks under
  `banks/desktop/`.
  The string table uses prefix compression. Thus `strings` gives fragments of the paths, not
  the full paths.

There is a smaller related problem. `get_baselib_reference` shows an `fmod_audio` topic, and
`get_modding_guide` shows an `audio` topic. The server rejects both topics as unknown.

### 12. AutoSlay plays no music at all, and it ignores `max_floor`

**Status:** idea, ours to fix (toolkit); both problems are cosmetic
**Evidence:** measurement on 2026-07-16 during a run (floor 29, act 2):
`AudioManagerProxy.music_track`, `MusicControllerProxy._musicEv`, `._currentTrack` and
`._ambienceEv` were all null

`AutoSlayer` sets `NonInteractiveMode.AutoSlayerCheck = () => IsActive`. Every method on
`NRunMusicController` returns immediately when `NonInteractiveMode.IsActive` is true, and
`StopMusic` is one of these methods. Thus an AutoSlay run has no act music and no ambience.
`NCharacterSelectScreen.BeginRun` correctly stops the menu music. The result is full silence.

This behavior is probably correct for a headless CI run. But the AutoSlay of this fork runs
balance batches that a person watches. Silence is not correct for that use. A Harmony
prefix can force `AutoSlayerCheck` to false for the music controller only. This restores the
music, and it keeps the suppression of the SFX and the VFX. This problem is different from the
`bridge_start_run` menu-music leak, which is fixed.

There is a second problem. `bridge_autoslay_configure` reports `applied: {max_floor: 2}`, but
the next run logs `Config maxFloor = 49`. Thus the override does not reach `AutoSlayConfig`,
and a short test run is not possible.

### 13. A publish under a live game corrupts its asset loads

**Status:** a warning decreases the risk; a full fix would restart the game automatically
**Evidence:** [troubleshooting.md](troubleshooting.md); observed 2026-07-16 as a score of
22/42 on code that scored 42/42 before a republish

The game keeps the pck file open. If you replace the pck at that time, every later asset load
from the pck throws an exception. This stops the setup of the combat UI, and the combat has no
background. The error message names the custom energy counter, which is not the cause.
`publish` now gives a warning when the game runs. But a user can miss the warning, and the
diagnosis of this failure takes a lot of time.

There are two options:

- `publish` offers to restart the game.
- The suite refuses to run when the pck is newer than the game process.

### 14. `potion_sell` fails intermittently in a full-suite run

**Status:** needs investigation
**Evidence:** observed 2026-07-16: the test failed one time in a full run with
`check 6 ... no new exceptions (got 1: Object reference not set to an instance of an object.)`.
The test passes every time with `--group shop`

The cause is the sequence or the state, not a fault in the code. The test passes alone, and it
also runs before the sweeps. Thus no later test in the suite can have an effect on it. At the
next failure, get the stack trace with `bridge_get_exceptions` immediately after the failure.
Do not run the test again until it passes. The suite now controls the releases. Thus this
failure stops a release.

### 15. CI cannot compile the mod

**Status:** will not fix, unless the constraint changes
**Evidence:** [.github/workflows/lint.yml](../.github/workflows/lint.yml); a build with an
incorrect `Sts2Path` stops with an error in `Sts2PathDiscovery.props`

CI runs the three-way lint, and it validates the localization JSON. CI cannot run
`dotnet build`, because the build needs `sts2.dll` from a Steam installation. A public runner
cannot have that installation. CI also cannot run the suite, because the suite needs a live
game. `scripts/dev.sh release` runs the compilation and the suite on the local machine
instead. This entry records the gap, because the gap is intentional.

### 16. Lint does not cover non-card entities

**Status:** idea, small
**Evidence:** [lint_sync.py](../scripts/lint_sync.py) reads only `cards.json`, as text

The three-way check applies to cards only. Relics, potions, powers, and enchantments get no
lint of their localization keys. The analyzer in the build covers `.smartDescription`, and
only for powers.

The check also skips all `WithCalculated*` formula cards. Its numeric comparison between the
literal and the CSV gives a warning only. The CI job now parses every localization file as
JSON, and `lint_sync.py` still does not do this itself.

---

## Release

### 17. The tooling does not do the Workshop upload

**Status:** idea
**Evidence:** [RELEASING.md](../RELEASING.md) says to add this content when it exists

`scripts/dev.sh release` does the preflight, the version bump, the changelog, the build, and
the zip file automatically. The GitHub Release is still a manual step. The Steam Workshop
upload is also a manual step. The change to `min_game_version` is also manual.
