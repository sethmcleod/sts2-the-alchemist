# Upstream improvements to BaseLib worth considering

Findings from Alchemist development worth contributing to
[BaseLib](https://github.com/Ekimekim/BaseLib).

Scope is BaseLib only. It's a hard dependency this mod can't work without and we don't
control it, so a fix has to land upstream or every player carries the bug. Tooling gaps in
[sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) don't belong here — we own
that fork and it's optional for playing this mod, so those are ours to fix directly.

Nothing here is filed yet. Land one PR at a time and confirm it merges before opening the
next — upstream responsiveness is unproven.

Each entry records the evidence it came from, so a stale one can be re-checked rather than
argued about.

---

### 1. Epoch packet serialization throws on epochs from uninstalled mods

**Status:** ready — patch written, verified live, shipped locally as `d465c65`
**Evidence:** [UnlockStateSerializationPatches.cs](../AlchemistCode/Patches/UnlockStateSerializationPatches.cs)

`SerializableUnlockState.Serialize` → `WriteEpochId` → `ModelIdSerializationCache.GetNetIdForEpochId`
throws `ArgumentException` for any epoch whose owning mod isn't loaded. That write happens
inside `CombatManager.EndCombatInternal` (replay write), so the throw aborts combat teardown
and the run wedges permanently on the killing blow — with no clue pointing at the real cause.

Worth upstreaming because our patch only protects players who have the Alchemist loaded; the
trap is armed for *any* mod that registers epochs and is later uninstalled, and it strands a
save with no in-game way out.

Note the asymmetry: the JSON load path already tolerates unknown epochs (non-fatal
`ValidationError` warnings), only the packet write throws. `ModelIdSerializationCache` has
`TryGetNetIdForCategory` and `TryGetNetIdForEntry` but **no `TryGetNetIdForEpochId`** — the
epoch path is the only one a caller can't probe. Adding that Try* variant is the cleaner fix
and arguably belongs with MegaCrit rather than BaseLib; the BaseLib patch is the pragmatic
version we can actually ship.

### 2. No public epoch/story registration API — mods must reflect into private statics

**Status:** idea
**Evidence:** [EpochRegistration.cs](../AlchemistCode/Epochs/EpochRegistration.cs)

Registering a custom epoch means writing to `EpochModel._allEpochs`, `._epochTypeDictionary`,
`._typeToIdDictionary`, nulling the `._allEpochIds` lazy cache, and `StoryModel._storyTypeDictionary`
— all private. Every epoch mod reimplements the same reflection, and one rename in a game
update breaks all of them at once. A `RegisterEpoch(Type)` / `RegisterStory(...)` that owns the
reflection in one place would centralize the blast radius.

Pairs naturally with #1: same subsystem, and #1's fix depends on registration having populated
`_allEpochs` before `ModelIdSerializationCache.Init()` runs.

### 3. `Skip*` prefixes strand vanilla epoch bookkeeping for custom characters

**Status:** needs investigation — confirm BaseLib's intent before filing
**Evidence:** header comment in [EpochPatches.cs](../AlchemistCode/Patches/EpochPatches.cs)

BaseLib's `Skip*` prefixes short-circuit vanilla epoch bookkeeping for custom characters, so
each mod re-awards its own epochs from postfixes on `ProgressSaveManager.ObtainCharUnlockEpoch`,
`CheckFifteenElitesDefeatedEpoch`, etc. — reaching private methods by reflection to do it.
Either a supported "award epoch X to this custom character" hook, or making `Skip*` opt-in,
would remove that. Low confidence this is unintended rather than a deliberate trade-off.

### 4. Sentry teardown hang affects any mod on v0.108.0-beta / macOS

**Status:** idea — may belong with MegaCrit instead
**Evidence:** `project-overview` memory; reproduced with BaseLib alone, no Alchemist

`SentryGodotLogger::_process_frame()` locks a destroyed mutex after the game shuts Sentry down
mid-load for modded runs → black screen or SIGABRT at load, with any mod installed. Today the
fix is a hand-written `override.cfg` next to the game executable, which every user has to
discover independently. BaseLib could apply the Sentry disable itself. Genuinely a MegaCrit
bug; BaseLib is just the only place we can neutralize it without waiting.
