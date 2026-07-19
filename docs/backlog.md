# Backlog

Improvements worth making that aren't scheduled yet. Balance passes and the character
artwork are tracked separately and deliberately absent here.

Scope is anything this repo could do. Two neighbours have their own lists: fixes that must
land in BaseLib live in [baselib-improvements.md](baselib-improvements.md), and known
gotchas that already have workarounds live in [troubleshooting.md](troubleshooting.md).
Toolkit gaps in [sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) are ours
to fix directly, so they're listed here with that noted.

Each entry records the evidence it came from, so a stale one can be re-checked rather than
argued about. Nothing here is committed to; ordering within a section is rough priority.

---

## Content and feel

### 1. Per-card SFX and VFX

**Status:** idea, unblocked, incremental
**Evidence:** 0 of 99 card files call `WithHitFx`/`WithAttackerFx`/`VfxCmd`; the only
`SfxCmd.Play` is a merchant line in [PotionSellPatches.cs](../AlchemistCode/Patches/PotionSellPatches.cs)

225 of 593 base-game card files (38%) attach explicit sound or visuals when played; the
Alchemist attaches none. The engine only covers card *movement* swooshes generically
(`SfxCmd.PlayCardSwooshSfx`), so impact and cast effects are per-card opt-in and default to
null. Needs no new assets: `MegaCrit.Sts2.Core.Audio/FmodSfx.cs` has 45 event constants
(`buff`, `debuff`, `heal`, `cardImpactIntoSingle`), `VfxCmd` has ~27 path constants, and
there are 167 `N*Vfx` classes including `NPoisonImpactVfx`, which base poison cards like
`BouncingFlask` already use. The poison cards alone are about an hour and get most of the
felt difference, so this can start anywhere and stop anywhere.

### 2. The rest of the character is still the Ironclad

**Status:** blocked on artwork
**Evidence:** [Alchemist.cs](../AlchemistCode/Character/Alchemist.cs) does not override
`PlaceholderID`, which `BaseLib.Abstracts.PlaceholderCharacterModel` defines as `"ironclad"`

The audio is fixed (`CustomAttackSfx`/`CustomCastSfx`/`CustomDeathSfx` now point at the
Silent and Necrobinder). Everything else `PlaceholderID` drives is still Ironclad and each
piece needs an asset: `CustomVisualPath` (the creature itself), `CustomTrailPath` (card-play
trail), `CustomRestSiteAnimPath`, `CustomMerchantAnimPath`, and `CustomArm*TexturePath` (the
multiplayer rock-paper-scissors hands, which matter because the mod ships 4 co-op cards).
Two more sfx hooks are also still inherited: `CharacterSelectSfx` and
`CharacterTransitionSfx`.

Once real art exists, overriding `PlaceholderID` itself may be simpler than overriding each
path, but that also re-points the sfx, so keep the audio overrides in mind.

### 3. Custom FMOD bank for bespoke audio

**Status:** idea, needs investigation
**Evidence:** banks load from a hardcoded `bank_paths` array on an `FmodBankLoader` node in
the game's main `.tscn`; `res://addons/fmod/` is a GDExtension

The real fix behind #1 and #2's audio. A mod could in principle instantiate its own
`FmodBankLoader` pointing at its own bank, but that needs authoring in FMOD Studio and
GUID-collision behavior is unverified. Cheaper alternative worth checking first: the base
game itself ships non-FMOD sounds via `NDebugAudioManager` playing plain Godot
`AudioStream`s from `res://debug_audio/` (81 card files reference `.mp3`, and that's what
`WithHitFx`'s third `tmpSfx` parameter is for). A mod pck can add `res://` paths and
`NDebugAudioManager.Instance.Play` is public.

### 4. A custom map event

**Status:** idea
**Evidence:** no `EventModel`/`ConstructedEventModel` anywhere in `AlchemistCode/`

The README leans on the character being woven into the world's timeline, and an event is
where that lands for players who don't read epoch text. The mod currently writes dialogue
for 8 base-game ancients but has no room of its own.

### 5. Other content types never touched

**Status:** idea
**Evidence:** no `MonsterModel`/`EncounterModel`/`OrbModel`/`ModifierModel`/`AncientModel`

Custom monsters, encounters, orbs, run modifiers, and custom ancients are all absent. Each
is a new subsystem with its own registration surface, so these are projects rather than
tasks. Listed for completeness, not because any is obviously worth doing.

### 6. Non-English localization

**Status:** idea, large but mechanical
**Evidence:** `Alchemist/localization/` contains only `eng/`

The 13 files are well-structured and the character keys are at full parity with the base
game, so this is translation work rather than refactoring. Worth noting the mod has no
hardcoded user-facing English in code.

---

## Testing and tooling

### 7. Multiplayer cards are excluded from every test

**Status:** ready, needs a co-op harness
**Evidence:** `MULTIPLAYER_CARDS = {"Bestow", "Effervesce", "Reflux", "Suffuse"}` at
[run_suite.py:72](../scripts/tests/run_suite.py)

The 4 co-op cards are hardcoded out of `cards_sweep` and covered by nothing else, so they're
the least-tested content in the mod. `Bestow` is the interesting one: it's the only caller of
`Infusion.InfuseRandomFromHand`, which no test reaches. Needs a second player, so it likely
means driving the game's multiplayer test scene (console `multiplayer test`).

### 8. `cards_sweep` silently tolerates unplayable cards

**Status:** ready, small
**Evidence:** [run_suite.py](../scripts/tests/run_suite.py) prints
`{skipped} unplayable-skipped` but never asserts on it

A card whose `can_play` is false at runtime is counted and printed, never failed. A card
could drift into being permanently unplayable and the sweep would still pass. Either assert
the count is 0 or pin an expected list.

### 9. What's left of the suite's runtime

**Status:** idea; diminishing returns, recorded so the dead ends aren't re-tried
**Evidence:** measured 2026-07-16. Full suite ~197s wall / 151s of scenarios, 0 restarts

Where the remaining time goes, and what does *not* move it:

- `cards_sweep` ~48s, of which 86 × ~270ms is the per-card `fight` transition. That fresh
  fight is what lets a failure name one card; reusing fights across cards would save ~20s but
  leak powers between them, so a throw could be blamed on the wrong card. Only worth it with a
  bisect step on failure.
- ~46s is inter-scenario: `reset_to_menu` (~3.2s) plus `start_run` (~3.2s) for the 11
  scenarios that can't batch. `"batch": "run"` is the lever, but only for genuinely
  self-contained scenarios (see `scripts/tests/README.md`).
- Every bridge call that touches the main thread costs ~16ms, and the suite makes ~10 per
  card. Combining reads (`_hand_index` re-reads state its caller just fetched) would save
  maybe 5-9s.

Measured dead ends: `--speed` above 3 does nothing (the fight transition is scene init, not
animation; 266ms at 3x vs 272ms at 20x), and raising the frame rate does nothing (uncapped +
vsync off left the 16ms floor unchanged; focused + uncapped was worse at 21ms).

### 10. The bridge can't see enchantments

**Status:** ready, ours to fix (toolkit)
**Evidence:** zero `enchant` hits in `test_mod/Code/BridgeHandler.cs`; hand cards serialize
`name`/`type`/`energy_cost`/`upgraded` only

The Infuse tests assert enchantments *by effect* (Exalted grants Strength, Fuming adds
Foul Vapor) because the bridge exposes no enchantment state. That's arguably better testing,
but it means Toxic can't be asserted directly at all: it applies Poison to an enemy, and
assertions can't read enemy power stacks either. Adding `enchantment`/`enchantment_amount`
to the hand-card payload would close that gap.

### 11. `list_game_audio` and `list_game_vfx` always return empty

**Status:** ready, ours to fix (toolkit)
**Evidence:** `_load_fmod_data()` at `sts2mcp/server.py` looks for `fmod_dump.json` in two
paths, finds neither, and silently returns `{"events": []}`

Every query returns 0 results while advertising "563 FMOD events across 12 banks", and
`get_setup_status` reports ready because it never checks. No `fmoddumper` mod exists in the
repo. This matters directly for #1. Two workarounds exist today:
`grep -rhoE '"event:/[^"]*"' decompiled` recovers 369 event paths, and the extracted game at
`~/Downloads/Slay the Spire 2/banks/desktop/` has the real banks (the string table is
prefix-compressed, so `strings` fragments the paths rather than yielding them whole).

Related and smaller: `get_baselib_reference` advertises an `fmod_audio` topic and
`get_modding_guide` an `audio` topic, and the server rejects both as unknown.

### 12. AutoSlay plays no music at all, and ignores `max_floor`

**Status:** idea, ours to fix (toolkit); both cosmetic
**Evidence:** measured 2026-07-16 mid-run (floor 29, act 2): `AudioManagerProxy.music_track`,
`MusicControllerProxy._musicEv`, `._currentTrack` and `._ambienceEv` all null

`AutoSlayer` wires `NonInteractiveMode.AutoSlayerCheck = () => IsActive`, and every method on
`NRunMusicController` (including `StopMusic`) early-returns when `NonInteractiveMode.IsActive`.
So an AutoSlay run has no act music and no ambience, while its menu music is correctly stopped
by `NCharacterSelectScreen.BeginRun`: net silence. That's presumably deliberate for headless
CI, but the fork's AutoSlay is for *watchable* balance batches, where silence is just wrong. A
Harmony prefix forcing `AutoSlayerCheck` false for the music controller only would restore it
without un-suppressing SFX/VFX. Not to be confused with the separate `bridge_start_run` menu-
music leak, which is fixed.

Also: `bridge_autoslay_configure` reports `applied: {max_floor: 2}` but the next run logs
`Config maxFloor = 49`, so the override never reaches `AutoSlayConfig`. Makes short test runs
impossible.

### 13. Publishing under a live game corrupts its asset loads

**Status:** mitigated by a warning; a real fix would auto-restart
**Evidence:** [troubleshooting.md](troubleshooting.md); observed 2026-07-16 as 22/42 passing
on code that scored 42/42 before a republish

Replacing the pck while the game holds it open makes every later asset load from it throw,
which aborts combat UI setup (no background) and blames the custom energy counter. `publish`
now warns when the game is up, but a warning is easy to scroll past and the failure mode is
expensive to diagnose. Options: have `publish` offer to restart the game, or have the suite
refuse to run when the pck is newer than the game process.

### 14. `potion_sell` flakes in a full-suite run

**Status:** needs investigation
**Evidence:** observed 2026-07-16: failed once in a full run with
`check 6 ... no new exceptions (got 1: Object reference not set to an instance of an object.)`,
passes consistently on `--group shop`

Order- or state-dependent rather than a code fault: it passes in isolation and runs before
the sweeps, so nothing later in the suite can be reaching it. Worth catching the stack trace
next time it fires (`bridge_get_exceptions` right after) rather than re-running until green.
Now that the suite gates releases, a flake here blocks a release.

### 15. CI can't compile

**Status:** wontfix unless the constraint changes
**Evidence:** [.github/workflows/lint.yml](../.github/workflows/lint.yml); building with a
bogus `Sts2Path` hard-fails out of `Sts2PathDiscovery.props`

CI runs the three-way lint and validates the localization JSON. It can't run `dotnet build`,
because that needs `sts2.dll` from a Steam install a public runner can't have, and it can't
run the suite, which needs a live game. Compilation and the suite are gated locally by
`scripts/dev.sh release` instead. Recorded so the gap isn't mistaken for an oversight.

### 16. Lint doesn't cover non-card entities

**Status:** idea, small
**Evidence:** [lint_sync.py](../scripts/lint_sync.py) reads only `cards.json`, as text

The three-way check is cards-only: relics, potions, powers, and enchantments get no loc-key
lint (the in-build analyzer covers `.smartDescription`, and only for powers). It also skips
`WithCalculated*` formula cards wholesale, and its literal-vs-csv numeric cross-check only
warns. The CI job now parses every localization file as JSON, which `lint_sync.py` still
doesn't do itself.

---

## Release

### 17. Workshop upload isn't wired into the tooling

**Status:** idea
**Evidence:** [RELEASING.md](../RELEASING.md) says to fill this in when it exists

`scripts/dev.sh release` automates preflight, bump, changelog, build, and zip. The
GitHub Release and the Steam Workshop upload are both still manual, as is bumping
`min_game_version`.
