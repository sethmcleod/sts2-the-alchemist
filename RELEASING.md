# Releasing The Alchemist

How versions are cut and shipped. The whole flow is one command,
`scripts/dev.sh release`, plus a manual git push you run yourself. If you only
read one thing: **every player-visible change earns a `CHANGELOG.md` entry**, and
each released changelog section becomes that version's Steam Workshop update note.

## Versioning policy

The version lives in one place, `Alchemist.json` (`"version": "vX.Y.Z"`), and is
bumped only by `scripts/dev.sh release`. Git tags match it (`vX.Y.Z`). We follow
[Semantic Versioning](https://semver.org), read for a game mod:

| Bump | When | Examples |
|---|---|---|
| **PATCH** (`0.1.0 → 0.1.1`) | Balance tweaks, bug fixes, text/tooltip/art fixes. Nothing new, nothing removed. | Re-cost a card, fix a Ferment interaction, reword a keyword. |
| **MINOR** (`0.1.0 → 0.2.0`) | New content or additive mechanics that don't break existing saves. | Add cards/relics/potions, a new epoch, a new keyword. |
| **MAJOR** (`0.x → 1.0`, then `1.x → 2.0`) | Save-breaking changes or identity-level reworks. **`1.0.0` is reserved for the first public Steam Workshop release.** | Remove/rename large card sets, overhaul a core mechanic. |

While pre-1.0 the mod is "feature-complete but not yet publicly released", so
prefer MINOR/PATCH and hold MAJOR for the 1.0 Workshop launch.

Two related manifest fields:

- **`min_game_version`**: bump by hand when a release requires a newer Slay the
  Spire 2 build.
- **BaseLib `min_version`**: do **not** edit by hand. It's auto-synced to the
  built-against BaseLib at build time (`Alchemist.csproj`, `UpdateDependencyVersions`).

## The changelog (hybrid workflow)

`CHANGELOG.md` is hand-curated in [Keep a Changelog](https://keepachangelog.com)
format, but [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)
do the first draft:

1. Write commits with Conventional Commit prefixes (`feat:`, `fix:`,
   `refactor:`, …). The changelog draft is only as good as the commit messages.
2. When preparing a release, run `scripts/dev.sh changelog`. It reads commits
   since the last tag and prints a grouped draft (Added / Fixed / Changed /
   Other). It writes nothing.
3. Paste the relevant lines under `## [Unreleased]` and **reword them into
   player-facing language**, so "feat: rework infuse enchantments" becomes
   "Infuse now grants type-matched enchantments that stack." Drop dev-only noise
   (build, ci, test, most chores). Group under the Keep a Changelog sections:
   Added, Changed, Deprecated, Removed, Fixed, Security.

The curated `## [Unreleased]` section is the release note. `release` cannot run
against an empty Unreleased section. No notes, no release.

## Cutting a release

```sh
scripts/dev.sh changelog        # draft; curate ## [Unreleased] in CHANGELOG.md by hand
scripts/dev.sh release minor    # or: patch | major | an explicit X.Y.Z
```

`release` (see `do_release` in `scripts/dev.sh`) will:

1. **Preflight**: refuse unless the working tree is clean and you're on `main`,
   run `dotnet build`, the `lint` check, and the regression suite, and refuse if
   `## [Unreleased]` is empty. The suite drives the live game, so Steam must be
   running. `--skip-tests` overrides it and ships the release unverified against
   the game; it prints a warning and is meant for the rare case where the game
   can't run, not for a red suite.
2. **Compute** the new version from the bump keyword (or take an explicit
   `X.Y.Z`) and confirm it's greater than the current one.
3. **Update** `Alchemist.json` (`version`) and `CHANGELOG.md` (stamp
   `## [Unreleased]` → `## [X.Y.Z] - <date>`, open a fresh Unreleased).
4. **Build & package** via `dotnet publish`, then write:
   - `dist/Alchemist-vX.Y.Z.zip`, the drop-in artifact (below).
   - `dist/RELEASE_NOTES-vX.Y.Z.txt`, this version's changelog section, for the
     GitHub Release body and the Workshop update note.
5. **Stop and print** the git block to run yourself (nothing is committed,
   tagged, or pushed for you):

   ```sh
   git add Alchemist.json CHANGELOG.md
   git commit -m "release: vX.Y.Z"
   git tag -a vX.Y.Z -m "vX.Y.Z"
   git push --follow-tags
   ```

Then create a GitHub Release for the tag, attach the zip, and paste the notes.

`dist/` is git-ignored. Artifacts are rebuilt, not committed.

## How players install it

**Steam Workshop is the preferred way to install and play.** It's one click, it
auto-updates, and it resolves the **BaseLib** dependency automatically.

> [!NOTE]
> Workshop release is coming soon; it's gated on the character artwork landing.
> Until then, use the manual zip below.

**Manual install (interim, from a GitHub Release zip):**

1. Install [**BaseLib**](https://github.com/Alchyr/BaseLib-StS2) first, since the
   Alchemist mod depends on it.
2. Download `Alchemist-vX.Y.Z.zip` and extract the `Alchemist/` folder into your
   game's `mods/` folder:
   - **macOS**: `…/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/`
   - **Windows/Linux**: the `mods/` folder next to the game executable.
3. Make sure your game is at least the manifest's `min_game_version`, then launch.

The zip contains exactly what the game loads (`Alchemist.dll`, `Alchemist.json`,
`Alchemist.pck`) under a single top-level `Alchemist/` folder. No repo clone, no
.NET or Godot needed. Developers building from source use `scripts/dev.sh
publish` instead, see [BUILD.md](BUILD.md).

## Steam Workshop mapping (for the 1.0 launch)

When we publish, each field has a home:

| Workshop item | Source |
|---|---|
| Update note (per version) | that version's `CHANGELOG.md` section / `dist/RELEASE_NOTES-*.txt` |
| Preview image | `Alchemist/mod_image.png` (already inside the `.pck`) |
| Description | `Alchemist.json` `description` + README highlights |
| Dependency | **BaseLib**, listed so Workshop auto-installs it |

The upload step itself isn't wired into tooling yet; fill this in when it is.
