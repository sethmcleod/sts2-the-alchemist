# Upstream improvements to BaseLib worth considering

These are findings from the development of the Alchemist mod. Each finding is a possible
contribution to [BaseLib](https://github.com/Alchyr/BaseLib-StS2).

The scope is BaseLib only. BaseLib is a hard dependency, and this mod cannot operate
without it. We do not control BaseLib. Thus a fix must go upstream, or every player keeps
the bug.

Do not put toolkit problems from
[sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) in this document. We
own that fork, and players do not need it. We fix those problems directly.

No item in this document is filed yet. Submit one PR at a time. Make sure that the PR
merges before you open the next PR. The response time of the upstream project is not
known.

Each entry records its evidence. Thus you can check an old entry again.

---

### 1. Epoch packet serialization throws on epochs from uninstalled mods

**Status:** ready. Patch written, verified live, shipped locally as `d465c65`
**Evidence:** [UnlockStateSerializationPatches.cs](../AlchemistCode/Patches/UnlockStateSerializationPatches.cs)

The call chain `SerializableUnlockState.Serialize` → `WriteEpochId` →
`ModelIdSerializationCache.GetNetIdForEpochId` throws an `ArgumentException`. This happens
for any epoch whose owner mod is not loaded. The write happens inside
`CombatManager.EndCombatInternal`, which is the replay write. The exception stops the
combat teardown. The run then stops permanently on the killing blow. The game gives no
message about the true cause.

This fix belongs upstream. Our patch protects only the players who load the Alchemist mod.
The same failure applies to *any* mod that registers epochs and is then uninstalled. The
failure leaves a save file with no solution in the game.

The two paths are not the same. The JSON load path accepts unknown epochs, and it gives
non-fatal `ValidationError` warnings. Only the packet write throws.
`ModelIdSerializationCache` has `TryGetNetIdForCategory` and `TryGetNetIdForEntry`, but it
has **no `TryGetNetIdForEpochId`**. Thus the epoch path is the only path that a caller
cannot test first.

A new `Try*` method is the better fix. That fix possibly belongs with MegaCrit, not with
BaseLib. The BaseLib patch is the practical version that we can release now.

### 2. No public epoch/story registration API, so mods must reflect into private statics

**Status:** idea
**Evidence:** [EpochRegistration.cs](../AlchemistCode/Epochs/EpochRegistration.cs)

To register a custom epoch, a mod must write to these private members:
`EpochModel._allEpochs`, `._epochTypeDictionary`, `._typeToIdDictionary`, and
`StoryModel._storyTypeDictionary`. The mod must also set the `._allEpochIds` lazy cache to
null. Every epoch mod writes the same reflection code again. One rename in a game update
breaks all of these mods at the same time.

A `RegisterEpoch(Type)` method and a `RegisterStory(...)` method could hold the reflection
in one place. Then a game update affects only that one place.

This item and item 1 go together, because they are in the same subsystem. Also, the fix
for item 1 needs `_allEpochs`. The registration must fill `_allEpochs` before
`ModelIdSerializationCache.Init()` runs.

### 3. `Skip*` prefixes strand vanilla epoch bookkeeping for custom characters

**Status:** needs investigation. Confirm BaseLib's intent before filing
**Evidence:** header comment in [EpochPatches.cs](../AlchemistCode/Patches/EpochPatches.cs)

The `Skip*` prefixes in BaseLib stop the vanilla epoch bookkeeping for custom characters.
Thus each mod must award its own epochs again. The mod does this from postfixes on
`ProgressSaveManager.ObtainCharUnlockEpoch`, `CheckFifteenElitesDefeatedEpoch`, and
related methods. The mod uses reflection to reach the private methods.

Two solutions can remove this work. The first solution is a supported hook that awards a
given epoch to a custom character. The second solution makes the `Skip*` prefixes opt-in.
This behavior is possibly a deliberate design decision, not a defect. The confidence in
this item is low.

### 4. Sentry teardown hang affects any mod on v0.108.0-beta / macOS

**Status:** idea, and it may belong with MegaCrit instead
**Evidence:** `project-overview` memory; reproduced with BaseLib alone, no Alchemist

For modded runs, the game shuts down Sentry during the load. After that,
`SentryGodotLogger::_process_frame()` locks a mutex that no longer exists. The result is a
black screen or a SIGABRT at load. This happens with any mod installed.

The current fix is a manual `override.cfg` file next to the game executable. Each user
must find this fix without help. BaseLib could disable Sentry itself. This is a MegaCrit
defect. However, BaseLib is the only place where we can correct it now.
